namespace TodoSample.Application.Todos;

using FluentValidation;
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
/// FluentValidation example for command-level rules over already-validated value objects.
/// </summary>
public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        // Wiring placeholder: add command-level or cross-field rules here after value objects are built.
        RuleFor(command => command.Title).NotNull();
        RuleFor(command => command.DueDate).NotNull();
    }
}

/// <summary>
/// Handler for CreateTodoCommand.
/// </summary>
public sealed class CreateTodoCommandHandler : ICommandHandler<CreateTodoCommand, Result<TodoItem>>
{
    private readonly ITodoRepository _repository;
    private readonly IActorProvider _actorProvider;
    private readonly TimeProvider _timeProvider;

    public CreateTodoCommandHandler(ITodoRepository repository, IActorProvider actorProvider, TimeProvider timeProvider)
    {
        _repository = repository;
        _actorProvider = actorProvider;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<TodoItem>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var actor = (await _actorProvider.GetCurrentActorAsync(cancellationToken))
            .GetValueOrThrow("Actor must be present; IAuthorize pipeline guarantees this.");
        var todo = new TodoItem(command.Title, command.DueDate, command.Tag, actor.Id, _timeProvider);
        return Result.Ok(todo)
            .Check(t => t.Start())
            .Tap(_repository.Add);
    }
}
