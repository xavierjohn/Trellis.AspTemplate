namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Completes a todo item. Only the creator can complete their own todo.
/// </summary>
public sealed record CompleteTodoCommand(TodoId TodoId) : ICommand<Result<TodoItem>>, IAuthorize, IAuthorizeResource<TodoItem>
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosComplete];

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

    public async ValueTask<Result<TodoItem>> Handle(CompleteTodoCommand command, CancellationToken cancellationToken)
    {
        var maybe = await _repository.FindByIdAsync(command.TodoId, cancellationToken);
        return await maybe
            .ToResult(Error.NotFound($"Todo {command.TodoId} not found."))
            .Bind(todo => todo.Complete().Map(_ => todo))
            .BindAsync(todo => _repository.SaveAsync(todo, cancellationToken).MapAsync(_ => todo));
    }
}
