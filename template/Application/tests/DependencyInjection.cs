namespace Application.Tests;

using Microsoft.Extensions.DependencyInjection;
using TodoSample.Application;
using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.Mediator;
using Trellis.Testing;

public static class DependencyInjection
{
    public static IServiceCollection AddMockDependencies(this IServiceCollection services)
    {
        var actorProvider = new TestActorProvider("test-user", Permissions.TodosCreate, Permissions.TodosRead, Permissions.TodosUpdate, Permissions.TodosComplete, Permissions.TodosDelete);
        services.AddSingleton<TestActorProvider>(actorProvider);
        services.AddSingleton<IActorProvider>(actorProvider);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<FakeRepository<TodoItem, TodoId>>();
        services.AddScoped<ITodoRepository, FakeRepositoryAdapter>();
        services.AddScoped<SharedResourceLoaderById<TodoItem, TodoId>, FakeTodoItemResourceLoader>();
        services.AddResourceAuthorization(typeof(CompleteTodoCommand).Assembly, typeof(FakeTodoItemResourceLoader).Assembly);
        return services;
    }
}
