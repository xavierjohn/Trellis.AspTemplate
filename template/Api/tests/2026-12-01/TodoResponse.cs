namespace Api.Tests._2026_12_01;

/// <summary>
/// Response model for deserialization in tests.
/// </summary>
public class TodoResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? CompletedAt { get; set; }
    public string? Tag { get; set; }
    public string CreatedByActorId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public bool IsOverdue { get; set; }
}
