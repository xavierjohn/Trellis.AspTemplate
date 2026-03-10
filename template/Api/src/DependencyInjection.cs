namespace BestWeatherForecast.Api;

using BestWeatherForecast.Api.Middleware;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ServiceLevelIndicators;
using Scalar.AspNetCore;
using Trellis.Asp;
using Asp.Versioning.Conventions;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.ConfigureOpenTelemetry();
        services.ConfigureServiceLevelIndicators();
        services.AddProblemDetails();
        services.AddControllers().AddScalarValueValidation();
        services.AddApiVersioning()
                .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
                .AddApiExplorer()
                .AddOpenApi(options => options.Document.AddScalarTransformers());
        services.AddScoped<ErrorHandlingMiddleware>();
        return services;
    }

    private static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services)
    {
        static void configureResource(ResourceBuilder r) => r.AddService(
            serviceName: "BestWeatherForecastService",
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
                builder.AddResultsInstrumentation();
                builder.AddPrimitiveValueObjectInstrumentation();
                builder.AddOtlpExporter();
            });

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
