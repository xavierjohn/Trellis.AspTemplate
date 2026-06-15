namespace TodoSample.Domain;

/// <summary>
/// Title of a todo item. 1–200 characters.
/// </summary>
[Trim, NotDefault, StringLength(200)]
public partial class Title : RequiredString<Title>;
