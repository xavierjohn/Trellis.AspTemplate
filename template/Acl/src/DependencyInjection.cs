namespace TodoSample.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoSample.Application;
using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;
using Trellis.Mediator;

public static class DependencyInjection
{
    public static IServiceCollection AddAntiCorruptionLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString)
                   .AddTrellisInterceptors());

        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<SharedResourceLoaderById<TodoItem, TodoId>, TodoItemResourceLoader>();
        services.AddResourceAuthorization(
            typeof(CompleteTodoCommand).Assembly,
            typeof(TodoItemResourceLoader).Assembly);
        services.AddTrellisUnitOfWork<AppDbContext>();

        return services;
    }
}
