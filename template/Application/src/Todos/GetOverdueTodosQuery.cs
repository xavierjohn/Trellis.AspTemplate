namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Gets a keyset-paginated page of overdue todo items, ordered by <see cref="TodoId"/>.
/// Returns a <see cref="Page{T}"/> so the controller can emit an RFC 8288 <c>Link</c> header
/// with <c>next</c> / <c>prev</c> relations.
/// </summary>
/// <remarks>
/// <c>Cursor</c> is an opaque continuation token echoed verbatim from the previous page's
/// <c>next</c> link (the framework encodes it via <c>CursorCodec</c>). A malformed cursor
/// surfaces as <c>422 Unprocessable Content</c>, never a stack trace.
/// </remarks>
public sealed record GetOverdueTodosQuery(string? Cursor, int Limit)
    : IQuery<Result<Page<TodoItem>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosRead];
}

/// <summary>
/// Handler for <see cref="GetOverdueTodosQuery"/>.
/// </summary>
public sealed class GetOverdueTodosQueryHandler : IQueryHandler<GetOverdueTodosQuery, Result<Page<TodoItem>>>
{
    private const int MaxLimit = 100;
    private const int DefaultLimit = 20;

    private readonly ITodoRepository _repository;
    private readonly TimeProvider _timeProvider;

    public GetOverdueTodosQueryHandler(ITodoRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<Page<TodoItem>>> Handle(GetOverdueTodosQuery query, CancellationToken cancellationToken)
    {
        var pageSize = PageSize.FromRequested(query.Limit <= 0 ? DefaultLimit : query.Limit, MaxLimit);
        var cursor = string.IsNullOrEmpty(query.Cursor) ? (Cursor?)null : new Cursor(query.Cursor);
        var spec = new OverdueTodoSpecification(_timeProvider.GetUtcNow().UtcDateTime);

        // The repository delegates to EF Core's ToPageAsync, which decodes the opaque cursor,
        // applies the keyset seek, over-fetches, and slices the page — surfacing a malformed
        // cursor as Error.InvalidInput ("cursor.malformed") => 422 without throwing.
        return await _repository.QueryPageAsync(spec, pageSize, cursor, cancellationToken);
    }
}

