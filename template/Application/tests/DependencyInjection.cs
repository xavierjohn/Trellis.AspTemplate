namespace Application.Tests;

using BestWeatherForecast.Application;
using BestWeatherForecast.Application.Todos;
using BestWeatherForecast.Domain;
using Microsoft.Extensions.DependencyInjection;
using Trellis.Authorization;
using Trellis.Mediator;
using Trellis.Testing.Fakes;

public static class DependencyInjection
{
    public static IServiceCollection AddMockDependencies(this IServiceCollection services)
    {
        var actorProvider = new TestActorProvider("test-user", Permissions.TodosCreate, Permissions.TodosRead, Permissions.TodosComplete, Permissions.TodosDelete);
        services.AddSingleton<TestActorProvider>(actorProvider);
        services.AddSingleton<IActorProvider>(actorProvider);
        services.AddSingleton<FakeRepository<TodoItem, TodoId>>();
        services.AddSingleton<ITodoRepository, FakeRepositoryAdapter>();
        services.AddResourceAuthorization(typeof(CompleteTodoCommand).Assembly, typeof(FakeCompleteTodoResourceLoader).Assembly);
        return services;
    }
}
