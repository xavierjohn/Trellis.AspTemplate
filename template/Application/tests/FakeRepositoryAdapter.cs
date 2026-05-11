namespace Application.Tests;

using TodoSample.Application;
using TodoSample.Domain;
using Trellis.Testing;

/// <summary>
/// Adapts <see cref="FakeRepository{TAggregate, TId}"/> to <see cref="ITodoRepository"/>.
/// <para>
/// Most members delegate directly to the fake — the surfaces are intentionally aligned.
/// Only <see cref="QueryPageAsync"/> is custom (keyset pagination) and is implemented here.
/// </para>
/// </summary>
internal class FakeRepositoryAdapter : ITodoRepository
{
    private readonly FakeRepository<TodoItem, TodoId> _repo;

    public FakeRepositoryAdapter(FakeRepository<TodoItem, TodoId> repo) => _repo = repo;

    public Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        _repo.FindByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<TodoItem>> QueryAsync(Specification<TodoItem> specification, CancellationToken cancellationToken) =>
        _repo.QueryAsync(specification, cancellationToken);

    public Task<(IReadOnlyList<TodoItem> Items, bool HasNext)> QueryPageAsync(
        Specification<TodoItem> specification,
        TodoId? afterId,
        int limit,
        CancellationToken cancellationToken)
    {
        var afterGuid = afterId is null ? (Guid?)null : (Guid)afterId;
        var query = _repo.GetAll()
            .Where(specification.IsSatisfiedBy)
            .OrderBy(t => (Guid)t.Id);

        var filtered = afterGuid is null
            ? (IEnumerable<TodoItem>)query
            : query.Where(t => (Guid)t.Id > afterGuid.Value);

        var rows = filtered.Take(limit + 1).ToList();
        var hasNext = rows.Count > limit;
        IReadOnlyList<TodoItem> items = hasNext ? rows.Take(limit).ToList() : rows;
        return Task.FromResult((items, hasNext));
    }

    public void Add(TodoItem todo) => _repo.Add(todo);

    public void Remove(TodoItem todo) => _repo.Remove(todo);

    public Task<Result<Unit>> RemoveByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        _repo.RemoveByIdAsync(id, cancellationToken);
}

