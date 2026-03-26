using BestWeatherForecast.AntiCorruptionLayer;
using BestWeatherForecast.Api;
using BestWeatherForecast.Api.Middleware;
using BestWeatherForecast.Application;
using Scalar.AspNetCore;
using ServiceLevelIndicators;
using Trellis.Asp;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentation(builder.Environment)
    .AddApplication()
    .AddAntiCorruptionLayer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=todos.db");

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
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

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseScalarValueValidation();
app.UseServiceLevelIndicator();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Main entry point for the application.
/// </summary>
public partial class Program
{
}
