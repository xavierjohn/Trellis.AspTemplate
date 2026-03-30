namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Creates a new todo item.
/// </summary>
public sealed record CreateTodoCommand(
    Title Title,
    DueDate DueDate,
    Maybe<Tag> Tag) : ICommand<Result<TodoItem>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosCreate];
}

/// <summary>
/// Handler for CreateTodoCommand.
/// </summary>
public sealed class CreateTodoCommandHandler : ICommandHandler<CreateTodoCommand, Result<TodoItem>>
{
    private readonly ITodoRepository _repository;
    private readonly IActorProvider _actorProvider;

    public CreateTodoCommandHandler(ITodoRepository repository, IActorProvider actorProvider)
    {
        _repository = repository;
        _actorProvider = actorProvider;
    }

    public async ValueTask<Result<TodoItem>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var actor = await _actorProvider.GetCurrentActorAsync(cancellationToken);
        return await TodoItem.TryCreate(command.Title, command.DueDate, command.Tag, actor.Id)
            .Bind(todo => todo.Start().Map(_ => todo))
            .CheckAsync(todo => _repository.SaveAsync(todo, cancellationToken));
    }
}
