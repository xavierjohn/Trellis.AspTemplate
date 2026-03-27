namespace TodoSample.Domain;

/// <summary>
/// Permission constants for todo operations.
/// </summary>
public static class Permissions
{
    public const string TodosCreate = "todos:create";
    public const string TodosRead = "todos:read";
    public const string TodosUpdate = "todos:update";
    public const string TodosComplete = "todos:complete";
    public const string TodosDelete = "todos:delete";
}
