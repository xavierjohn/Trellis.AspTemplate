namespace TodoSample.Domain;

/// <summary>
/// Title of a todo item. 1–200 characters.
/// </summary>
[StringLength(200), Trim, NotDefault]
public partial class Title : RequiredString<Title>
{
}
