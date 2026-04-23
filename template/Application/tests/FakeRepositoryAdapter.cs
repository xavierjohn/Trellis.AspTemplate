namespace Application.Tests;

using TodoSample.Application;
using TodoSample.Domain;
using Trellis.Testing;

/// <summary>
/// Adapts FakeRepository to the ITodoRepository interface.
/// </summary>
internal class FakeRepositoryAdapter : ITodoRepository
{
    private readonly FakeRepository<TodoItem, TodoId> _repo;

    public FakeRepositoryAdapter(FakeRepository<TodoItem, TodoId> repo) => _repo = repo;

    public async Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken)
    {
        var result = await _repo.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Maybe.From(result.Unwrap()) : Maybe<TodoItem>.None;
    }

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(Specification<TodoItem> specification, CancellationToken cancellationToken)
    {
        var items = _repo.GetAll().Where(specification.IsSatisfiedBy).ToList();
        return Task.FromResult<IReadOnlyList<TodoItem>>(items);
    }

    public Task<Result<Page<TodoItem>>> GetPageAsync(int requestedLimit, Cursor? cursor, CancellationToken cancellationToken)
    {
        const int ServerCap = 100;
        var appliedLimit = Math.Min(requestedLimit <= 0 ? 10 : requestedLimit, ServerCap);

        Guid? afterId = null;
        if (cursor is { } c)
        {
            if (!Guid.TryParse(c.Token, out var decoded))
                return Task.FromResult(Result.Fail<Page<TodoItem>>(
                    new Error.BadRequest("invalid_cursor") { Detail = "Cursor is not a recognized token." }));
            afterId = decoded;
        }

        var ordered = _repo.GetAll()
            .OrderBy(t => t.Id.Value)
            .Where(t => afterId is not Guid g || t.Id.Value.CompareTo(g) > 0)
            .Take(appliedLimit + 1)
            .ToList();

        var hasMore = ordered.Count > appliedLimit;
        var items = hasMore ? ordered.Take(appliedLimit).ToList() : ordered;
        Cursor? next = hasMore && items.Count > 0 ? new Cursor(items[^1].Id.Value.ToString()) : null;

        return Task.FromResult(Result.Ok(new Page<TodoItem>(
            Items: items,
            Next: next,
            Previous: null,
            RequestedLimit: requestedLimit,
            AppliedLimit: appliedLimit)));
    }

    public async Task<Result> SaveAsync(TodoItem todo, CancellationToken cancellationToken) =>
        await _repo.SaveAsync(todo, cancellationToken);

    public async Task<Result> DeleteAsync(TodoId id, CancellationToken cancellationToken) =>
        await _repo.DeleteAsync(id, cancellationToken);
}
