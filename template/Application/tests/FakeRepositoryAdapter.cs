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

    public Task<Result<Page<TodoItem>>> QueryPageAsync(
        Specification<TodoItem> specification,
        PageSize pageSize,
        Cursor? cursor,
        CancellationToken cancellationToken)
    {
        // The real repository delegates to EF Core's ToPageAsync; the in-memory fake mirrors
        // its contract with the storage-agnostic CursorCodec + PageBuilder primitives.
        Guid? afterId = null;
        if (cursor is { } token)
        {
            var decoded = CursorCodec.TryDecode<Guid>(token);
            if (!decoded.TryGetValue(out var afterGuid, out var cursorError))
                return Task.FromResult(Result.Fail<Page<TodoItem>>(cursorError));
            afterId = afterGuid;
        }

        var ordered = _repo.GetAll()
            .Where(specification.IsSatisfiedBy)
            .OrderBy(t => (Guid)t.Id);

        var seeked = afterId is { } cursorId
            ? ordered.Where(t => (Guid)t.Id > cursorId)
            : (IEnumerable<TodoItem>)ordered;

        var rows = seeked.Take(pageSize.Applied + 1).ToList();
        var page = PageBuilder.FromOverFetch(rows, pageSize, t => (Guid)t.Id);
        return Task.FromResult(Result.Ok(page));
    }

    public void Add(TodoItem todo) => _repo.Add(todo);

    public void Remove(TodoItem todo) => _repo.Remove(todo);

    public Task<Result<Unit>> RemoveByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        _repo.RemoveByIdAsync(id, cancellationToken);
}

