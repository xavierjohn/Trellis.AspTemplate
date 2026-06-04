namespace TodoSample.Application;

using TodoSample.Domain;

/// <summary>
/// Repository interface for TodoItem persistence.
/// <para>
/// Mirrors the public surface of <c>RepositoryBase&lt;TodoItem, TodoId&gt;</c> from
/// <c>Trellis.EntityFrameworkCore</c> so that <c>FakeRepository&lt;TodoItem, TodoId&gt;</c>
/// from <c>Trellis.Testing</c> shape-matches this contract directly. Handlers stage changes
/// via <see cref="Add"/> / <see cref="Remove"/> / <see cref="RemoveByIdAsync"/>;
/// <c>TransactionalCommandBehavior</c> (registered by <c>AddTrellisUnitOfWork&lt;AppDbContext&gt;()</c>)
/// commits exactly once on handler success. Do <b>not</b> add a <c>SaveAsync</c> member here:
/// that name belongs to the <c>FakeRepository</c> test convenience surface, not to a
/// production handler contract.
/// </para>
/// </summary>
public interface ITodoRepository
{
    /// <summary>Finds a todo by ID. Returns <see cref="Maybe{T}.None"/> if not found.</summary>
    Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken);

    /// <summary>Returns all todos satisfying the specification.</summary>
    Task<IReadOnlyList<TodoItem>> QueryAsync(Specification<TodoItem> specification, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a keyset-paginated page of todos matching the specification, ordered by Id.
    /// The implementation peeks one extra row to determine whether a next page exists.
    /// </summary>
    /// <param name="specification">The specification to filter by.</param>
    /// <param name="afterId">Exclusive lower bound — the Id from the previous page's last item, or <c>null</c> for the first page.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (items up to <paramref name="limit"/>, hasNext flag).</returns>
    Task<(IReadOnlyList<TodoItem> Items, bool HasNext)> QueryPageAsync(
        Specification<TodoItem> specification,
        TodoId? afterId,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>Stages an aggregate for insertion. The unit-of-work commits on handler success.</summary>
    void Add(TodoItem todo);

    /// <summary>Stages an aggregate for deletion. The unit-of-work commits on handler success.</summary>
    void Remove(TodoItem todo);

    /// <summary>
    /// Loads the aggregate by id and stages it for deletion in one call.
    /// Returns <see cref="Error.NotFound"/> if absent. The unit-of-work commits on handler success.
    /// </summary>
    Task<Result<Unit>> RemoveByIdAsync(TodoId id, CancellationToken cancellationToken);
}

