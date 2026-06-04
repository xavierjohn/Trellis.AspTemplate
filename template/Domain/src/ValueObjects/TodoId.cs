namespace TodoSample.Domain;

/// <summary>
/// Unique identifier for a todo item.
/// </summary>
public partial class TodoId : RequiredGuid<TodoId>
{
    static partial void ValidateAdditional(Guid value, string fieldName, ref string? errorMessage)
    {
        if (value == Guid.Empty)
            errorMessage = "Todo Id cannot be empty.";
    }
}
