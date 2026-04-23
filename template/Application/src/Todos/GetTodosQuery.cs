namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Lists todos as a single cursor-paginated page. Caps the requested limit server-side and
/// surfaces the framework's <see cref="Page{T}"/> envelope so the controller emits an
/// RFC 8288 <c>Link</c> header for the <c>next</c> page.
/// </summary>
/// <param name="Limit">Requested page size; coerced to a sensible default if missing.</param>
/// <param name="CursorToken">Opaque continuation token from a previous response; <c>null</c> for the first page.</param>
public sealed record GetTodosQuery(int? Limit, string? CursorToken) : IQuery<Result<Page<TodoItem>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosRead];
}

/// <summary>
/// Handler for <see cref="GetTodosQuery"/>.
/// </summary>
public sealed class GetTodosQueryHandler : IQueryHandler<GetTodosQuery, Result<Page<TodoItem>>>
{
    private const int DefaultLimit = 10;

    private readonly ITodoRepository _repository;

    public GetTodosQueryHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<Page<TodoItem>>> Handle(GetTodosQuery query, CancellationToken cancellationToken)
    {
        var requestedLimit = query.Limit is int l && l > 0 ? l : DefaultLimit;
        Cursor? cursor = string.IsNullOrEmpty(query.CursorToken) ? null : new Cursor(query.CursorToken);
        return await _repository.GetPageAsync(requestedLimit, cursor, cancellationToken);
    }
}
