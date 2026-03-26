namespace BestWeatherForecast.Api.v2026_03_26.Models;

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

    /// <summary>Maps from application DTO to API response.</summary>
    public static TodoResponse From(Application.Todos.TodoDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        DueDate = dto.DueDate,
        Status = dto.Status,
        CompletedAt = dto.CompletedAt,
        Tag = dto.Tag,
        CreatedByActorId = dto.CreatedByActorId,
        CreatedAt = dto.CreatedAt
    };
}
