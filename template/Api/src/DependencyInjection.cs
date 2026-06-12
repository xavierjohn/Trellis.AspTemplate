namespace TodoSample.Api;

using System.Diagnostics;
using Asp.Versioning.Conventions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;
using Trellis.ServiceLevelIndicators;
using Trellis.Asp;
using Trellis.Asp.Authorization;
using Trellis.Asp.Idempotency;
using TodoSample.Domain;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IHostEnvironment environment)
    {
        services.ConfigureOpenTelemetry();
        services.ConfigureServiceLevelIndicators();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                // Always surface the active trace id so clients can correlate the error with
                // server-side spans / log entries. Falls back to the ASP.NET connection-level
                // trace identifier when no diagnostic Activity is current.
                var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
                ctx.ProblemDetails.Extensions["traceId"] = traceId;

                // For 500 responses do not leak raw exception detail to the client; replace the
                // default detail with a support-friendly message that nudges the user toward
                // filing a ticket with the trace id.
                if (ctx.ProblemDetails.Status == StatusCodes.Status500InternalServerError)
                {
                    ctx.ProblemDetails.Detail =
                        "An error occurred in our API. Please refer the trace id with our support team.";
                }

                // RFC 9110 §15.5.6: the Allow header lists methods the resource supports.
                // ASP.NET routing already emits the header on a 405; surface it in the body
                // as a structured array so clients that ignore response headers still discover
                // the supported methods.
                if (ctx.ProblemDetails.Status == StatusCodes.Status405MethodNotAllowed &&
                    ctx.HttpContext.Response.Headers.TryGetValue("Allow", out var allow))
                {
                    // RFC 9110 §5.6.1: Allow is comma-separated with optional whitespace.
                    // Split on ',' and trim so a server that emits "GET,PUT,DELETE" (no spaces)
                    // surfaces three array entries, not one combined string.
                    ctx.ProblemDetails.Extensions["allow"] = allow.ToString()
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            };
        });
        services.AddControllers();
        services.AddTrellisAspWithScalarValidation();
        services.AddResourceCollectionName<TodoItem>("todos");
        services.AddTrellisIdempotency();
        services.AddInMemoryIdempotencyStore();
        services.AddApiVersioning()
                .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
                .AddApiExplorer()
                .AddOpenApi(options => options.Document.AddScalarTransformers());
        services.AddHealthChecks();

        if (environment.IsDevelopment())
            services.AddDevelopmentActorProvider();
        else
            throw new InvalidOperationException(
                "Production IActorProvider not configured. " +
                "Register AddEntraActorProvider() with your Azure Entra ID configuration for non-development environments.");

        return services;
    }

    private static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services)
    {
        static void configureResource(ResourceBuilder r) => r.AddService(
            serviceName: "TodoSampleService",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

        services.AddOpenTelemetry()
            .ConfigureResource(configureResource)
            .WithMetrics(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddServiceLevelIndicatorInstrumentation();
                builder.AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http");
                builder.AddOtlpExporter();
            })
            .WithTracing(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddPrimitiveValueObjectInstrumentation();
                // Trellis.Mediator's TracingBehavior emits a span per command/query from the
                // "Trellis.Mediator" ActivitySource (TracingBehavior<,>.ActivitySourceName).
                // Register it so each handler shows in the trace next to the HTTP and
                // value-object spans; without it those command/query spans are dropped.
                builder.AddSource("Trellis.Mediator");
                builder.AddOtlpExporter();
            });

        // Export ILogger logs over OTLP so they appear in the Aspire dashboard's Structured
        // Logs view (and any OTLP backend), correlated to traces by traceId/spanId. Without
        // this only metrics and traces are exported and the logs view stays empty.
        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            // Export structured ILogger state (e.g. LogInformation("User {UserId}", id)) as
            // OTLP log attributes so the Structured Logs view shows the key/value pairs, not
            // just the formatted message.
            options.ParseStateValues = true;
            var resourceBuilder = ResourceBuilder.CreateDefault();
            configureResource(resourceBuilder);
            options.SetResourceBuilder(resourceBuilder);
            options.AddOtlpExporter();
        }));

        return services;
    }

    private static IServiceCollection ConfigureServiceLevelIndicators(this IServiceCollection services)
    {
        services.AddServiceLevelIndicator(options =>
        {
            options.LocationId = ServiceLevelIndicator.CreateLocationId("public", "westus3");
        })
        .AddMvc()
        .AddApiVersion();

        return services;
    }
}
