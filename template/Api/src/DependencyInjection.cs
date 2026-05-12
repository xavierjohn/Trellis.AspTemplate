namespace TodoSample.Api;

using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Trellis.ServiceLevelIndicators;
using TodoSample.Api.Middleware;
using Trellis.Asp;
using Trellis.Asp.Authorization;

internal static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IHostEnvironment environment)
    {
        services.ConfigureOpenTelemetry();
        services.ConfigureServiceLevelIndicators();
        services.AddProblemDetails();
        services.AddControllers().AddScalarValueValidation();
        // Attribute-based API versioning: controllers declare [ApiVersion("...")] explicitly so a single
        // controller class can serve multiple versions without folder/namespace duplication.
        // Do NOT add VersionByNamespaceConvention — see template/.github/copilot-instructions.md
        // "API versioning approach" for the rationale.
        //
        // UnsupportedApiVersionStatusCode = 404: when an endpoint exists at version v2 but the client
        // requests it under v1 (or vice versa), Asp.Versioning's default is 400 Bad Request. 404 is the
        // semantically correct response — the request syntax is valid; the resource doesn't exist at
        // that version. 404 also matches what a typo'd path returns, giving clients one consistent
        // signal for "no such resource at this surface."
        services.AddApiVersioning(options =>
                {
                    options.UnsupportedApiVersionStatusCode = StatusCodes.Status404NotFound;
                })
                .AddMvc()
                .AddApiExplorer()
                .AddOpenApi(options => options.Document.AddScalarTransformers());
        services.AddScoped<ErrorHandlingMiddleware>();
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
