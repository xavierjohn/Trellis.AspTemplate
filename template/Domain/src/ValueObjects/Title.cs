namespace TodoSample.Domain;

/// <summary>
/// Title of a todo item. 1–200 characters.
/// </summary>
[StringLength(200)]
public partial class Title : RequiredString<Title>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            errorMessage = "Title cannot be empty or whitespace.";
    }
}
