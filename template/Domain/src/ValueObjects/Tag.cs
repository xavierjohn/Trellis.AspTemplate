namespace TodoSample.Domain;

using System.Text.RegularExpressions;

/// <summary>
/// A tag for categorizing todo items. Lowercase alphanumeric and hyphens only, 1–50 characters.
/// </summary>
[StringLength(50)]
public partial class Tag : RequiredString<Tag>
{
    static partial void ValidateAdditional(string value, string fieldName, ref string? errorMessage)
    {
        if (!TagPattern().IsMatch(value))
            errorMessage = "Tag must contain only lowercase letters, numbers, and hyphens.";
    }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex TagPattern();
}
