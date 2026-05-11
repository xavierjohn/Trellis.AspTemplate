namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Deletes a todo item. Only the creator can delete their own todo.
/// </summary>
public sealed record DeleteTodoCommand(TodoId TodoId) : ICommand<Result<Trellis.Unit>>, IAuthorize, IAuthorizeResource<TodoItem>, IIdentifyResource<TodoItem, TodoId>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosDelete];

    /// <inheritdoc />
    public IResult Authorize(Actor actor, TodoItem resource) =>
        Result.Ensure(actor.IsOwner(resource.CreatedByActorId),
            new Error.Forbidden("todo.delete.creator-only", new ResourceRef("Todo", resource.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))) { Detail = "Only the creator can delete this todo." });

    /// <inheritdoc />
    public TodoId GetResourceId() => TodoId;
}

/// <summary>
/// Handler for DeleteTodoCommand.
/// </summary>
public sealed class DeleteTodoCommandHandler : ICommandHandler<DeleteTodoCommand, Result<Trellis.Unit>>
{
    private readonly ITodoRepository _repository;

    public DeleteTodoCommandHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<Trellis.Unit>> Handle(DeleteTodoCommand command, CancellationToken cancellationToken) =>
        await _repository.RemoveByIdAsync(command.TodoId, cancellationToken);
}
