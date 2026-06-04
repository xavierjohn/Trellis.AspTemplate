using Asp.Versioning;
using Scalar.AspNetCore;
using Trellis.ServiceLevelIndicators;
using TodoSample.AntiCorruptionLayer;
using TodoSample.Api;
using TodoSample.Application;
using Trellis.Asp;
using Trellis.Asp.Idempotency;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentation(builder.Environment)
    .AddApplication()
    .AddAntiCorruptionLayer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=todos.db");

var app = builder.Build();

// Create database schema in development (use migrations in production)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(
        options =>
        {
            var descriptions = app.DescribeApiVersions();

            for (var i = 0; i < descriptions.Count; i++)
            {
                var description = descriptions[i];
                var isDefault = i == descriptions.Count - 1;
                options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
            }
        });
}

// Error-handling pipeline — placed before all downstream middleware so 4xx/5xx responses
// from anywhere in the pipeline produce an RFC 9457 ProblemDetails body. UseExceptionHandler
// converts unhandled exceptions (thrown by endpoints, middleware, filters) into 500
// ProblemDetails via IProblemDetailsService. UseStatusCodePages converts empty-body 4xx/5xx
// responses written by ASP.NET-native pipeline short-circuits (404 route-miss, 405
// MethodNotAllowed, 406 NotAcceptable, 413 ContentTooLarge, 415 UnsupportedMediaType) into
// ProblemDetails as well. The bodies are enriched in DependencyInjection.AddProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseTrellisIdempotency();
app.UseScalarValueValidation();
app.UseServiceLevelIndicator();
app.MapControllers();
// /health is a cross-cutting infra endpoint — it must respond to liveness/readiness probes
// regardless of which API version a client speaks. Tagging it explicitly api-version-neutral
// (rather than relying on it being implicitly outside the MVC versioning pipeline) makes
// `?api-version` truly optional, surfaces it as `Neutral` rather than `Unspecified` in the
// SLI/OpenTelemetry tags, and documents the intent for future readers. We attach the metadata
// directly because `IsApiVersionNeutral()` requires an associated `WithApiVersionSet(...)`,
// which doesn't apply to non-versioned endpoints like health checks.
app.MapHealthChecks("/health").WithMetadata(new ApiVersionNeutralAttribute());

app.Run();

/// <summary>
/// Main entry point for the application.
/// </summary>
public partial class Program
{
}
