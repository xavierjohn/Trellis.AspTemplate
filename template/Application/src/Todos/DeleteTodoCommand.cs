namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Deletes a todo item.
/// <para>
/// Destructive mutation. Requires <c>If-Match</c> (RFC 6585) to prevent the
/// lost-update race: the client must have read the resource first and present
/// its ETag, so a concurrent mutation between read and delete surfaces as
/// <c>412 Precondition Failed</c> instead of silently destroying newer state.
/// </para>
/// </summary>
public sealed record DeleteTodoCommand : ICommand<Result<Trellis.Unit>>, IAuthorize
{
    public TodoId TodoId { get; }

    /// <summary>
    /// The ETag from the client's <c>If-Match</c> header.
    /// <para>
    /// Required (RFC 6585). When the array is <c>null</c>, the handler returns
    /// <c>new Error.TransportFault(new HttpError.PreconditionRequired(...))</c> which surfaces as
    /// <c>428 Precondition Required</c>. When provided, the handler validates it against the
    /// aggregate's current ETag before deletion, returning <c>412 Precondition Failed</c> if
    /// stale (RFC 9110).
    /// </para>
    /// </summary>
    public EntityTagValue[]? IfMatchETags { get; }

    public DeleteTodoCommand(TodoId todoId, EntityTagValue[]? ifMatchETags = null)
    {
        TodoId = todoId;
        IfMatchETags = ifMatchETags;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosDelete];
}

/// <summary>
/// Handler for DeleteTodoCommand.
/// </summary>
public sealed class DeleteTodoCommandHandler : ICommandHandler<DeleteTodoCommand, Result<Trellis.Unit>>
{
    private readonly ITodoRepository _repository;

    public DeleteTodoCommandHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<Trellis.Unit>> Handle(DeleteTodoCommand command, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(command.TodoId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<TodoItem>(command.TodoId)) { Detail = $"Todo {command.TodoId} not found." })
            .RequireETagAsync(command.IfMatchETags)
            .TapAsync(_repository.Remove)
            .MapAsync(_ => Trellis.Unit.Value);
}
