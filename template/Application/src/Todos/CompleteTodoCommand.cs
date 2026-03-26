namespace TodoSample.Application.Todos;

using TodoSample.Domain;
using Mediator;
using Trellis.Authorization;

/// <summary>
/// Completes a todo item. Only the creator can complete their own todo.
/// </summary>
public sealed record CompleteTodoCommand(TodoId TodoId) : ICommand<Result<TodoItem>>, IAuthorizeResource<TodoItem>
{
    /// <inheritdoc />
    public IResult Authorize(Actor actor, TodoItem resource) =>
        Result.Ensure(actor.IsOwner(resource.CreatedByActorId),
            Error.Forbidden("Only the creator can complete this todo."));
}

/// <summary>
/// Handler for CompleteTodoCommand.
/// </summary>
public sealed class CompleteTodoCommandHandler : ICommandHandler<CompleteTodoCommand, Result<TodoItem>>
{
    private readonly ITodoRepository _repository;

    public CompleteTodoCommandHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<TodoItem>> Handle(CompleteTodoCommand command, CancellationToken cancellationToken) =>
        await _repository.GetByIdAsync(command.TodoId, cancellationToken)
            .BindAsync(todo => todo.Complete().Map(_ => todo))
            .TapAsync(todo => _repository.SaveAsync(todo, cancellationToken));
}
