namespace TodoSample.Application;

using Microsoft.Extensions.DependencyInjection;
using TodoSample.Application.Todos;
using Trellis.FluentValidation;
using Trellis.Mediator;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddTrellisBehaviors();
        services.AddTrellisFluentValidation(typeof(CreateTodoCommandValidator).Assembly);
        return services;
    }
}
