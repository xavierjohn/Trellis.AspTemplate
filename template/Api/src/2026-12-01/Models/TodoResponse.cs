namespace TodoSample.Api.v2026_12_01.Models;

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
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the todo was last modified.</summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// True when the todo is past its due date and still open (Active or Pending).
    /// <para>
    /// Added in v2026-12-01. v1 callers continue to receive the original response shape
    /// — this is the canonical example of namespace-versioned response evolution.
    /// </para>
    /// </summary>
    public bool IsOverdue { get; init; }

    /// <summary>Maps from domain aggregate to API response.</summary>
    public static TodoResponse From(TodoItem todo, DateTime now) => new()
    {
        Id = todo.Id.Value,
        Title = todo.Title.Value,
        DueDate = todo.DueDate.Value,
        Status = todo.Status.Value,
        CompletedAt = todo.CompletedAt.AsNullable(),
        Tag = todo.Tag.Match<string?>(t => t.Value, () => null),
        CreatedByActorId = todo.CreatedByActorId,
        CreatedAt = todo.CreatedAt,
        LastModified = todo.LastModified,
        IsOverdue = (todo.Status == TodoStatus.Active || todo.Status == TodoStatus.Pending)
            && todo.DueDate.Value < now
    };
}
