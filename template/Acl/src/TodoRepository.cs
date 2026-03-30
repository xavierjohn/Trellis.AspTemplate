namespace TodoSample.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using TodoSample.Application;
using TodoSample.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of ITodoRepository.
/// </summary>
internal class TodoRepository : ITodoRepository
{
    private readonly AppDbContext _context;

    public TodoRepository(AppDbContext context) => _context = context;

    public async Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        await _context.TodoItems.FirstOrDefaultMaybeAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(Specification<TodoItem> specification, CancellationToken cancellationToken) =>
        await _context.TodoItems
            .Where(specification)
            .ToListAsync(cancellationToken);

    public async Task<Result<Unit>> SaveAsync(TodoItem todo, CancellationToken cancellationToken)
    {
        var entry = _context.Entry(todo);
        if (entry.State == EntityState.Detached)
            _context.TodoItems.Add(todo);

        return await _context.SaveChangesResultUnitAsync(cancellationToken);
    }

    public async Task<Result<Unit>> DeleteAsync(TodoId id, CancellationToken cancellationToken)
    {
        var maybe = await FindByIdAsync(id, cancellationToken);
        return await maybe
            .ToResult(Error.NotFound($"Todo {id} not found."))
            .Tap(todo => _context.TodoItems.Remove(todo))
            .BindAsync(_ => _context.SaveChangesResultUnitAsync(cancellationToken));
    }
}
