namespace TodoSample.AntiCorruptionLayer;

using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Shared resource loader for TodoItem authorization.
/// </summary>
internal sealed class TodoItemResourceLoader : SharedResourceLoaderById<TodoItem, TodoId>
{
    private readonly AppDbContext _context;

    public TodoItemResourceLoader(AppDbContext context) => _context = context;

    public override async Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        await _context.TodoItems
            .Where(t => t.Id == id)
            .FirstOrDefaultResultAsync(
                new Error.NotFound(ResourceRef.For<TodoItem>(id)) { Detail = $"Todo item {id.Value} not found." },
                cancellationToken);
}
