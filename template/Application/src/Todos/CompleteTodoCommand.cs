namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Completes a todo item. Only the creator can complete their own todo.
/// <para>
/// Body-less state-transition POST. Does <strong>not</strong> require <c>If-Match</c>:
/// the state machine guard on <see cref="TodoItem.Complete"/> already rejects stale
/// transitions (e.g., completing an already-completed todo) with
/// <c>422 Unprocessable Content</c> — there is no body to overwrite, so a precondition
/// header would be ceremony without benefit. See the template's
/// "Require <c>If-Match</c> on body-overwriting mutations" rule for the full decision table.
/// </para>
/// </summary>
public sealed record CompleteTodoCommand : ICommand<Result<TodoItem>>, IAuthorize, IAuthorizeResource<TodoItem>, IIdentifyResource<TodoItem, TodoId>
{
    public TodoId TodoId { get; }

    public CompleteTodoCommand(TodoId todoId)
    {
        TodoId = todoId;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosComplete];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, TodoItem resource) =>
        Result.Ensure(actor.IsOwner(resource.CreatedByActorId),
            Error.Forbidden.For<TodoItem>("todo.complete.creator-only", resource.Id, "Only the creator can complete this todo."));

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

    public async ValueTask<Result<TodoItem>> Handle(CompleteTodoCommand command, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(command.TodoId, cancellationToken)
            .ToResultAsync(Error.NotFound.For<TodoItem>(command.TodoId, $"Todo {command.TodoId} not found."))
            .CheckAsync(todo => todo.Complete(_timeProvider));
}
