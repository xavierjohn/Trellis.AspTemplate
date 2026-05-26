namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Completes a todo item. Only the creator can complete their own todo.
/// <para>
/// State-transition POST. Requires <c>If-Match</c> (RFC 6585) to prevent the
/// lost-update race: the client must have read the resource first and present
/// its ETag, so a concurrent mutation between read and complete surfaces as
/// <c>412 Precondition Failed</c> instead of silently winning.
/// </para>
/// </summary>
public sealed record CompleteTodoCommand : ICommand<Result<TodoItem>>, IAuthorize, IAuthorizeResource<TodoItem>, IIdentifyResource<TodoItem, TodoId>
{
    public TodoId TodoId { get; }

    /// <summary>
    /// The ETag from the client's <c>If-Match</c> header.
    /// <para>
    /// Required (RFC 6585). When the array is <c>null</c>, the handler returns
    /// <c>new Error.TransportFault(new HttpError.PreconditionRequired(...))</c> which surfaces as
    /// <c>428 Precondition Required</c>. When provided, the handler validates it against the
    /// aggregate's current ETag before mutation, returning <c>412 Precondition Failed</c> if
    /// stale (RFC 9110).
    /// </para>
    /// </summary>
    public EntityTagValue[]? IfMatchETags { get; }

    public CompleteTodoCommand(TodoId todoId, EntityTagValue[]? ifMatchETags = null)
    {
        TodoId = todoId;
        IfMatchETags = ifMatchETags;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosComplete];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, TodoItem resource) =>
        Result.Ensure(actor.IsOwner(resource.CreatedByActorId),
            new Error.Forbidden("todo.complete.creator-only", new ResourceRef("Todo", resource.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))) { Detail = "Only the creator can complete this todo." });

    /// <inheritdoc />
    public TodoId GetResourceId() => TodoId;
}

/// <summary>
/// Handler for CompleteTodoCommand.
/// </summary>
public sealed class CompleteTodoCommandHandler : ICommandHandler<CompleteTodoCommand, Result<TodoItem>>
{
    private readonly ITodoRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteTodoCommandHandler(ITodoRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<TodoItem>> Handle(CompleteTodoCommand command, CancellationToken cancellationToken)
    {
        var maybe = await _repository.FindByIdAsync(command.TodoId, cancellationToken);
        return maybe
            .ToResult(new Error.NotFound(new ResourceRef("Todo", command.TodoId.ToString(System.Globalization.CultureInfo.InvariantCulture))) { Detail = $"Todo {command.TodoId} not found." })
            .RequireETag(command.IfMatchETags)
            .Bind(todo => todo.Complete(_timeProvider).Map(_ => todo));
    }
}
