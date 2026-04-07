namespace Domain.Tests;

using TodoSample.Domain;

public class TodoItemTests
{
    private static Title TestTitle => Title.Create("Buy groceries");
    private static DueDate FutureDueDate => DueDate.Create(DateTime.UtcNow.AddDays(7));
    private static DueDate PastDueDate => DueDate.Create(DateTime.UtcNow.AddDays(-1));

    private static TodoItem CreateActiveTodo(DueDate? dueDate = null, Maybe<Tag>? tag = null, string actorId = "actor-1")
    {
        var result = TodoItem.TryCreate(TestTitle, dueDate ?? FutureDueDate, tag ?? Maybe<Tag>.None, actorId);
        result.Should().BeSuccess();
        var todo = result.Unwrap();
        todo.Start().Should().BeSuccess();
        return todo;
    }

    [Fact]
    public void TryCreate_valid_todo_returns_pending_state()
    {
        var dueDate = FutureDueDate;
        var result = TodoItem.TryCreate(TestTitle, dueDate, Maybe<Tag>.None, "actor-1");

        result.Should().BeSuccess();
        var todo = result.Unwrap();
        todo.Title.Should().Be(TestTitle);
        todo.DueDate.Should().Be(dueDate);
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CreatedByActorId.Should().Be("actor-1");
        todo.CompletedAt.Should().BeNone();
    }

    [Fact]
    public void TryCreate_with_tag_preserves_tag()
    {
        var tag = Tag.Create("work");

        var result = TodoItem.TryCreate(TestTitle, FutureDueDate, Maybe.From(tag), "actor-1");

        result.Should().BeSuccess()
            .Which.Tag.Should().HaveValueEqualTo(tag);
    }

    [Fact]
    public void TryCreate_raises_TodoCreated_event()
    {
        var result = TodoItem.TryCreate(TestTitle, FutureDueDate, Maybe<Tag>.None, "actor-1");

        result.Should().BeSuccess();
        result.Unwrap().UncommittedEvents().Should().ContainSingle()
            .Which.Should().BeOfType<TodoCreated>();
    }

    [Fact]
    public void Start_from_Pending_transitions_to_Active()
    {
        var createResult = TodoItem.TryCreate(TestTitle, FutureDueDate, Maybe<Tag>.None, "actor-1");
        createResult.Should().BeSuccess();
        var todo = createResult.Unwrap();

        var startResult = todo.Start();

        startResult.Should().BeSuccess()
            .Which.Should().Be(TodoStatus.Active);
        todo.Status.Should().Be(TodoStatus.Active);
    }

    [Fact]
    public void Complete_from_Active_transitions_to_Completed()
    {
        var todo = CreateActiveTodo();

        var result = todo.Complete();

        result.Should().BeSuccess()
            .Which.Should().Be(TodoStatus.Completed);
        todo.Status.Should().Be(TodoStatus.Completed);
        todo.CompletedAt.Should().HaveValue();
    }

    [Fact]
    public void Complete_raises_TodoCompleted_event()
    {
        var todo = CreateActiveTodo();
        todo.AcceptChanges();

        todo.Complete().Should().BeSuccess();

        todo.UncommittedEvents().Should().ContainSingle()
            .Which.Should().BeOfType<TodoCompleted>();
    }

    [Fact]
    public void Complete_from_Pending_fails()
    {
        var result = TodoItem.TryCreate(TestTitle, FutureDueDate, Maybe<Tag>.None, "actor-1");
        result.Should().BeSuccess();

        result.Unwrap().Complete().Should().BeFailure();
    }

    [Fact]
    public void Start_from_Active_fails()
    {
        var todo = CreateActiveTodo();

        todo.Start().Should().BeFailure();
    }

    [Fact]
    public void IsOverdue_active_past_due_returns_true()
    {
        var todo = CreateActiveTodo(PastDueDate);

        todo.IsOverdue(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_active_future_due_returns_false()
    {
        var todo = CreateActiveTodo(FutureDueDate);

        todo.IsOverdue(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_pending_past_due_returns_false()
    {
        var result = TodoItem.TryCreate(TestTitle, PastDueDate, Maybe<Tag>.None, "actor-1");
        result.Should().BeSuccess();

        result.Unwrap().IsOverdue(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Update_active_todo_succeeds()
    {
        var todo = CreateActiveTodo();
        var newTitle = Title.Create("Updated title");
        var newDueDate = DueDate.Create(DateTime.UtcNow.AddDays(14));
        var newTag = Maybe.From(Tag.Create("updated"));

        var result = todo.Update(newTitle, newDueDate, newTag);

        result.Should().BeSuccess();
        todo.Title.Should().Be(newTitle);
        todo.DueDate.Should().Be(newDueDate);
        todo.Tag.Should().HaveValueEqualTo(Tag.Create("updated"));
    }

    [Fact]
    public void Update_completed_todo_fails()
    {
        var todo = CreateActiveTodo();
        todo.Complete().Should().BeSuccess();

        var result = todo.Update(Title.Create("Too late"), FutureDueDate, Maybe<Tag>.None);

        result.Should().BeFailure();
    }
}
