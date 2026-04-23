namespace TodoSample.Application;

using Microsoft.Extensions.DependencyInjection;
using Trellis.FluentValidation;
using Trellis.Mediator;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        // AddTrellisBehaviors() registers the canonical (Exception, Tracing, Logging,
        // Authorization, Validation) pipeline. AddTrellisFluentValidation() plugs the
        // FluentValidation adapter into the Validation stage; failures aggregate into a
        // single Error.UnprocessableContent response.
        services.AddTrellisBehaviors();
        services.AddTrellisFluentValidation(typeof(DependencyInjection).Assembly);
        return services;
    }
}
