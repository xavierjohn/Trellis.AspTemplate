namespace TodoSample.Domain;

using Trellis.Stateless;

/// <summary>
/// A todo item aggregate with state machine lifecycle management.
/// </summary>
public partial class TodoItem : Aggregate<TodoId>
{
    private static class Triggers
    {
        public const string Start = "Start";
        public const string Complete = "Complete";
    }

    private readonly LazyStateMachine<TodoStatus, string> _machine;

    /// <summary>The title of this todo.</summary>
    public Title Title { get; private set; } = null!;

    /// <summary>The due date for this todo.</summary>
    public DueDate DueDate { get; private set; } = null!;

    /// <summary>Current lifecycle status.</summary>
    public TodoStatus Status { get; private set; }

    /// <summary>When the todo was completed, if applicable.</summary>
    public partial Maybe<DateTime> CompletedAt { get; private set; }

    /// <summary>Optional categorization tag.</summary>
    public partial Maybe<Tag> Tag { get; private set; }

    /// <summary>The actor who created this todo.</summary>
    public string CreatedByActorId { get; private set; } = null!;

    /// <summary>When the todo was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>EF Core constructor.</summary>
    private TodoItem() : base(default!)
    {
        _machine = new LazyStateMachine<TodoStatus, string>(
            () => Status,
            s => Status = s,
            ConfigureStateMachine);
    }

    private TodoItem(Title title, DueDate dueDate, Maybe<Tag> tag, string createdByActorId)
        : base(TodoId.NewUniqueV7())
    {
        Title = title;
        DueDate = dueDate;
        Tag = tag;
        Status = TodoStatus.Pending;
        CreatedByActorId = createdByActorId;
        CreatedAt = DateTime.UtcNow;

        _machine = new LazyStateMachine<TodoStatus, string>(
            () => Status,
            s => Status = s,
            ConfigureStateMachine);

        DomainEvents.Add(new TodoCreated(Id, title, createdByActorId, CreatedAt));
    }

    /// <summary>
    /// Creates a new todo item in Pending state.
    /// </summary>
    public static Result<TodoItem> TryCreate(Title title, DueDate dueDate, Maybe<Tag> tag, string createdByActorId) =>
        new TodoItem(title, dueDate, tag, createdByActorId);

    /// <summary>
    /// Starts the todo, transitioning from Pending to Active.
    /// </summary>
    public Result<TodoStatus> Start() =>
        _machine.FireResult(Triggers.Start);

    /// <summary>
    /// Completes the todo, transitioning from Active to Completed.
    /// </summary>
    public Result<TodoStatus> Complete() =>
        _machine.FireResult(Triggers.Complete)
            .Tap(_ =>
            {
                var completedAt = DateTime.UtcNow;
                CompletedAt = completedAt;
                DomainEvents.Add(new TodoCompleted(Id, completedAt));
            });

    /// <summary>
    /// Returns true if this todo is overdue (active and past its due date).
    /// </summary>
    public bool IsOverdue(DateTime asOf) =>
        Status == TodoStatus.Active && DueDate < asOf;

    /// <summary>
    /// Updates the todo's title, due date, and tag. Cannot update completed todos.
    /// </summary>
    public Result<TodoItem> Update(Title title, DueDate dueDate, Maybe<Tag> tag) =>
        Status == TodoStatus.Completed
            ? Result.Failure<TodoItem>(Error.Domain("Cannot update a completed todo."))
            : Result.Success(this)
                .Tap(_ =>
                {
                    Title = title;
                    DueDate = dueDate;
                    Tag = tag;
                });

    private static void ConfigureStateMachine(Stateless.StateMachine<TodoStatus, string> machine)
    {
        machine.Configure(TodoStatus.Pending)
            .Permit(Triggers.Start, TodoStatus.Active);

        machine.Configure(TodoStatus.Active)
            .Permit(Triggers.Complete, TodoStatus.Completed);

        machine.Configure(TodoStatus.Completed);
    }
}
