namespace TodoSample.Domain;

/// <summary>
/// Due date for a todo item.
/// </summary>
public partial class DueDate : RequiredDateTime<DueDate>
{
    static partial void ValidateAdditional(DateTime value, string fieldName, ref string? errorMessage)
    {
        if (value == DateTime.MinValue)
            errorMessage = "Due Date cannot be DateTime.MinValue.";
    }
}
