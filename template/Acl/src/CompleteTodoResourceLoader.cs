namespace TodoSample.AntiCorruptionLayer;

using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Resource loader for CompleteTodoCommand — loads TodoItem by ID from the repository.
/// </summary>
internal class CompleteTodoResourceLoader : ResourceLoaderById<CompleteTodoCommand, TodoItem, TodoId>
{
    private readonly AppDbContext _context;

    public CompleteTodoResourceLoader(AppDbContext context) => _context = context;

    protected override TodoId GetId(CompleteTodoCommand message) => message.TodoId;

    protected override async Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        await _context.TodoItems
            .Where(t => t.Id == id)
            .FirstOrDefaultResultAsync(
                new Error.NotFound(new ResourceRef("Todo", id.ToString(System.Globalization.CultureInfo.InvariantCulture))) { Detail = $"Todo item {id.Value} not found." },
                cancellationToken);
}
