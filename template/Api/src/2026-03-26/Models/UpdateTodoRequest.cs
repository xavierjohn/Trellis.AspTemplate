namespace TodoSample.Api.v2026_03_26.Models;

using TodoSample.Domain;

/// <summary>
/// Request model for updating a todo item.
/// </summary>
public record UpdateTodoRequest
{
    /// <summary>Updated title (1–200 characters).</summary>
    public Title Title { get; init; } = null!;

    /// <summary>Updated due date (must be in the future).</summary>
    public DueDate DueDate { get; init; } = null!;

    /// <summary>Updated optional categorization tag.</summary>
    public Maybe<Tag> Tag { get; init; }
}
