namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Updates a todo item's title, due date, and tag.
/// </summary>
/// <remarks>
/// Validation lives in <see cref="UpdateTodoCommandValidator"/> (FluentValidation), which the
/// Mediator pipeline runs before this command reaches the handler. The handler returns a
/// <see cref="WriteOutcome{T}"/> so idempotent re-submits short-circuit to
/// <see cref="WriteOutcome{T}.UpdatedNoContent"/> (HTTP 204) without a redundant write,
/// while a real change returns <see cref="WriteOutcome{T}.Updated"/> (HTTP 200) with the new
/// representation in the body.
/// </remarks>
public sealed record UpdateTodoCommand(
    TodoId TodoId,
    Title Title,
    DueDate DueDate,
    Maybe<Tag> Tag,
    EntityTagValue[]? IfMatchETags = null) : ICommand<Result<WriteOutcome<TodoItem>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosUpdate];
}

/// <summary>
/// Handler for <see cref="UpdateTodoCommand"/>.
/// </summary>
public sealed class UpdateTodoCommandHandler : ICommandHandler<UpdateTodoCommand, Result<WriteOutcome<TodoItem>>>
{
    private readonly ITodoRepository _repository;

    public UpdateTodoCommandHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<WriteOutcome<TodoItem>>> Handle(UpdateTodoCommand command, CancellationToken cancellationToken)
    {
        var maybe = await _repository.FindByIdAsync(command.TodoId, cancellationToken);
        return await maybe
            .ToResult(new Error.NotFound(new ResourceRef("Todo", command.TodoId.Value.ToString())) { Detail = $"Todo {command.TodoId} not found." })
            .OptionalETag(command.IfMatchETags)
            .Bind<TodoItem, WriteOutcome<TodoItem>>(todo =>
                IsUnchanged(todo, command)
                    ? Result.Ok<WriteOutcome<TodoItem>>(new WriteOutcome<TodoItem>.UpdatedNoContent())
                    : todo.Update(command.Title, command.DueDate, command.Tag)
                          .Map(updated => (WriteOutcome<TodoItem>)new WriteOutcome<TodoItem>.Updated(updated)))
            .CheckAsync(outcome => outcome is WriteOutcome<TodoItem>.Updated updated
                ? _repository.SaveAsync(updated.Value, cancellationToken)
                : Task.FromResult(Result.Ok()));
    }

    private static bool IsUnchanged(TodoItem todo, UpdateTodoCommand command) =>
        todo.Title == command.Title
        && todo.DueDate == command.DueDate
        && todo.Tag == command.Tag;
}
