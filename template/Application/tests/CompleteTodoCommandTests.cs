namespace Application.Tests;

using Mediator;
using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Testing;

public class CompleteTodoCommandTests
{
    private readonly ISender _sender;
    private readonly FakeRepository<TodoItem, TodoId> _repo;
    private readonly TestActorProvider _actorProvider;

    public CompleteTodoCommandTests(ISender sender, FakeRepository<TodoItem, TodoId> repo, TestActorProvider actorProvider)
    {
        _sender = sender;
        _repo = repo;
        _actorProvider = actorProvider;
    }

    [Fact]
    public async Task Complete_own_todo_succeeds()
    {
        var createResult = await _sender.Send(
            new CreateTodoCommand(Title.Create("My todo"), DueDate.Create(DateTime.UtcNow.AddDays(1)), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);
        createResult.Should().BeSuccess();
        var created = createResult.Unwrap();

        var result = await _sender.Send(
            new CompleteTodoCommand(created.Id),
            TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        var todo = result.Unwrap();
        todo.Status.Should().Be(TodoStatus.Completed);
        todo.CompletedAt.Should().HaveValue();
    }

    [Fact]
    public async Task Complete_another_users_todo_returns_forbidden()
    {
        await using var _ = _actorProvider.WithActor("user-1", Permissions.TodosCreate, Permissions.TodosComplete);
        var createResult = await _sender.Send(
            new CreateTodoCommand(Title.Create("User1 todo"), DueDate.Create(DateTime.UtcNow.AddDays(1)), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);
        createResult.Should().BeSuccess();
        var created = createResult.Unwrap();

        await using var scope = _actorProvider.WithActor("user-2", Permissions.TodosComplete);
        var result = await _sender.Send(new CompleteTodoCommand(created.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.Forbidden>();
    }
}
