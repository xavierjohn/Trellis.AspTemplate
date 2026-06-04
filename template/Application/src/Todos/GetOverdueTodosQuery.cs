namespace TodoSample.Application.Todos;

using System.Globalization;
using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Gets a keyset-paginated page of overdue todo items, ordered by <see cref="TodoId"/>.
/// Returns a <see cref="Page{T}"/> so the controller can emit an RFC 8288 <c>Link</c> header
/// with <c>next</c> / <c>prev</c> relations.
/// </summary>
/// <remarks>
/// Cursor format is the next item's <see cref="Guid"/> formatted as <c>"N"</c> (32 hex chars, no hyphens).
/// A malformed cursor surfaces as <c>422 Unprocessable Content</c>, never a stack trace.
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
        var requested = query.Limit <= 0 ? DefaultLimit : query.Limit;
        var applied = Math.Clamp(requested, 1, MaxLimit);

        TodoId? afterId = null;
        if (query.Cursor is not null)
        {
            if (!Guid.TryParseExact(query.Cursor, "N", out var afterGuid))
                return Result.Fail<Page<TodoItem>>(
                    Error.InvalidInput.ForField("cursor", "cursor.malformed", "Cursor is not a valid opaque token."));

            // Bypass TryCreate's "not Guid.Empty" rule via the malformed-cursor channel.
            // Guid.TryParseExact accepts the all-zero GUID syntactically, but TodoId rejects
            // it as a domain value — an all-zero cursor is a malformed token, not a missing
            // cursor, so surface it as 422 rather than letting TryCreate throw via .Unwrap().
            var todoIdResult = TodoId.TryCreate(afterGuid);
            if (!todoIdResult.TryGetValue(out var todoId))
                return Result.Fail<Page<TodoItem>>(
                    Error.InvalidInput.ForField("cursor", "cursor.malformed", "Cursor is not a valid opaque token."));

            afterId = todoId;
        }

        var spec = new OverdueTodoSpecification(_timeProvider.GetUtcNow().UtcDateTime);
        var (items, hasNext) = await _repository.QueryPageAsync(spec, afterId, applied, cancellationToken);

        Cursor? next = hasNext && items.Count > 0
            ? new Cursor(((Guid)items[^1].Id).ToString("N", CultureInfo.InvariantCulture))
            : null;

        // This sample uses a forward-only keyset cursor (meaning "items with Id > X").
        // That format cannot self-describe a backward link, so `Previous` is always null.
        // A production API needing reverse pagination would encode direction in the cursor token
        // (e.g. "after:{id}" / "before:{id}") and run a descending query for the prev page.
        return Result.Ok(new Page<TodoItem>(
            Items: items,
            Next: next,
            Previous: null,
            RequestedLimit: requested,
            AppliedLimit: applied));
    }
}

