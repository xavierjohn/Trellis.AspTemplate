namespace TodoSample.Api.v2026_03_26.Models;

using TodoSample.Domain;

/// <summary>
/// Response model for a todo item.
/// </summary>
public record TodoResponse
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Title of the todo.</summary>
    public string Title { get; init; } = null!;

    /// <summary>Due date.</summary>
    public DateTime DueDate { get; init; }

    /// <summary>Current status (Pending, Active, Completed).</summary>
    public string Status { get; init; } = null!;

    /// <summary>When the todo was completed, if applicable.</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Optional categorization tag.</summary>
    public string? Tag { get; init; }

    /// <summary>Actor who created this todo.</summary>
    public string CreatedByActorId { get; init; } = null!;

    /// <summary>When the todo was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Maps from domain aggregate to API response.</summary>
    public static TodoResponse From(TodoItem todo) => new()
    {
        Id = todo.Id.Value,
        Title = todo.Title.Value,
        DueDate = todo.DueDate.Value,
        Status = todo.Status.ToString(),
        CompletedAt = todo.CompletedAt.AsNullable(),
        Tag = todo.Tag.Match<string?>(t => t.Value, () => null),
        CreatedByActorId = todo.CreatedByActorId,
        CreatedAt = todo.CreatedAt
    };
}
