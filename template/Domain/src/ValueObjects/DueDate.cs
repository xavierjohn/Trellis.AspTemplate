namespace TodoSample.Domain;

/// <summary>
/// Due date for a todo item.
/// </summary>
[NotDefault]
public partial class DueDate : RequiredDateTime<DueDate>;
