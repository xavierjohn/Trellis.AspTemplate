namespace TodoSample.Application;

using Microsoft.Extensions.DependencyInjection;
using TodoSample.Application.Todos;
using Trellis.Mediator;
using Trellis.Mediator.FluentValidation;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddTrellisBehaviors();
        services.AddDomainEventDispatch(typeof(CreateTodoCommandHandler).Assembly);
        services.AddTrellisFluentValidation(typeof(CreateTodoCommandValidator).Assembly);
        return services;
    }
}
