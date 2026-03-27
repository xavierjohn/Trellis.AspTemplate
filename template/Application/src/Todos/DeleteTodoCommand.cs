namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Deletes a todo item.
/// </summary>
public sealed record DeleteTodoCommand(TodoId TodoId) : ICommand<Result<Trellis.Unit>>, IAuthorize
{
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
        await _repository.DeleteAsync(command.TodoId, cancellationToken);
}
