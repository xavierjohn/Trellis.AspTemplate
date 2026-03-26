namespace BestWeatherForecast.AntiCorruptionLayer;

using BestWeatherForecast.Application;
using BestWeatherForecast.Domain;
using Microsoft.EntityFrameworkCore;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of ITodoRepository.
/// </summary>
internal class TodoRepository : ITodoRepository
{
    private readonly AppDbContext _context;

    public TodoRepository(AppDbContext context) => _context = context;

    public async Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        await _context.TodoItems
            .FirstOrDefaultResultAsync(
                t => t.Id == id,
                Error.NotFound($"Todo item {id.Value} not found."),
                cancellationToken)
            .ConfigureAwait(false);

    public async Task<Result<IReadOnlyList<TodoItem>>> GetAllAsync(Specification<TodoItem> specification, CancellationToken cancellationToken)
    {
        var items = await _context.TodoItems
            .Where(specification)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<TodoItem>>(items);
    }

    public async Task<Result<Unit>> SaveAsync(TodoItem todo, CancellationToken cancellationToken)
    {
        var entry = _context.Entry(todo);
        if (entry.State == EntityState.Detached)
            _context.TodoItems.Add(todo);

        return await _context.SaveChangesResultUnitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<Unit>> DeleteAsync(TodoId id, CancellationToken cancellationToken) =>
        await GetByIdAsync(id, cancellationToken)
            .TapAsync(todo =>
            {
                _context.TodoItems.Remove(todo);
                return Task.CompletedTask;
            })
            .BindAsync(_ => _context.SaveChangesResultUnitAsync(cancellationToken))
            .ConfigureAwait(false);
}
