namespace TodoSample.Domain;

/// <summary>
/// Represents the lifecycle state of a todo item.
/// </summary>
public enum TodoStatus
{
    /// <summary>Todo has been created but not started.</summary>
    Pending,

    /// <summary>Todo is actively being worked on.</summary>
    Active,

    /// <summary>Todo has been completed.</summary>
    Completed
}
