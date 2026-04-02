namespace TodoSample.Domain;

/// <summary>
/// Represents the lifecycle state of a todo item.
/// </summary>
public partial class TodoStatus : RequiredEnum<TodoStatus>
{
    /// <summary>Todo has been created but not started.</summary>
    public static readonly TodoStatus Pending = new();

    /// <summary>Todo is actively being worked on.</summary>
    public static readonly TodoStatus Active = new();

    /// <summary>Todo has been completed.</summary>
    public static readonly TodoStatus Completed = new();
}
