namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Completes a todo item. Only the creator can complete their own todo.
/// </summary>
public sealed record CompleteTodoCommand(TodoId TodoId) : ICommand<Result<TodoItem>>, IAuthorize, IAuthorizeResource<TodoItem>, IIdentifyResource<TodoItem, TodoId>
{
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
            .Bind(todo => todo.Complete(_timeProvider).Map(_ => todo));
    }
}
