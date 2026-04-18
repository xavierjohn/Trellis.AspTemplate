namespace TodoSample.AntiCorruptionLayer;

using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Shared resource loader for TodoItem — loads by ID for all commands that implement
/// <see cref="IIdentifyResource{TResource, TId}"/>. One loader per aggregate type
/// replaces per-command loaders.
/// </summary>
internal sealed class TodoItemResourceLoader(AppDbContext context)
    : SharedResourceLoaderById<TodoItem, TodoId>
{
    public override async Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        await context.TodoItems
            .FirstOrDefaultResultAsync(
                t => t.Id == id,
                Error.NotFound($"Todo item {id.Value} not found."),
                cancellationToken);
}
