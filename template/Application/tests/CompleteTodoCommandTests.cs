namespace Application.Tests;

using Mediator;
using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Testing.Fakes;

#pragma warning disable TRLS003 // Tests assert success before accessing .Value

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
        // Create a todo as the current actor
        var createResult = await _sender.Send(
            new CreateTodoCommand(Title.Create("My todo"), DueDate.Create(DateTime.UtcNow.AddDays(1)), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);
        createResult.Should().BeSuccess();

        // Complete it
        var result = await _sender.Send(new CompleteTodoCommand(createResult.Value.Id), TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Value.Status.Should().Be(TodoStatus.Completed);
        result.Value.CompletedAt.Should().HaveValue();
    }

    [Fact]
    public async Task Complete_another_users_todo_returns_forbidden()
    {
        // Create a todo as user-1
        await using var _ = _actorProvider.WithActor("user-1", Permissions.TodosCreate, Permissions.TodosComplete);
        var createResult = await _sender.Send(
            new CreateTodoCommand(Title.Create("User1 todo"), DueDate.Create(DateTime.UtcNow.AddDays(1)), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);
        createResult.Should().BeSuccess();

        // Switch to user-2 and try to complete
        await using var scope = _actorProvider.WithActor("user-2", Permissions.TodosComplete);
        var result = await _sender.Send(new CompleteTodoCommand(createResult.Value.Id), TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<ForbiddenError>();
    }
}
