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
        var maybeActor = await _actorProvider.GetCurrentActorAsync(cancellationToken);
        if (!maybeActor.TryGetValue(out var actor))
            return Result.Fail<TodoItem>(new Error.Unauthorized() { Detail = "No authenticated actor." });

        return TodoItem.TryCreate(command.Title, command.DueDate, command.Tag, actor.Id, _timeProvider)
            .Bind(todo => todo.Start().Map(_ => todo))
            .Tap(_repository.Add);
    }
}
