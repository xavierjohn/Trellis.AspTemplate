namespace TodoSample.Domain;

/// <summary>
/// Unique identifier for a todo item.
/// </summary>
[NotDefault]
public partial class TodoId : RequiredGuid<TodoId>;
