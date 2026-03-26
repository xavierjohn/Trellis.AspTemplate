namespace TodoSample.Application;

using Microsoft.Extensions.DependencyInjection;
using Trellis.Mediator;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddTrellisBehaviors();
        return services;
    }
}
