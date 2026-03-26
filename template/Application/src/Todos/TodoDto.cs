namespace BestWeatherForecast.Application.Todos;

using BestWeatherForecast.Domain;

/// <summary>
/// DTO for todo item responses.
/// </summary>
public sealed record TodoDto(
    Guid Id,
    string Title,
    DateTime DueDate,
    string Status,
    DateTime? CompletedAt,
    string? Tag,
    string CreatedByActorId,
    DateTime CreatedAt)
{
    public static TodoDto From(TodoItem todo) => new(
        todo.Id.Value,
        todo.Title.Value,
        todo.DueDate.Value,
        todo.Status.ToString(),
        todo.CompletedAt.Match<DateTime?>(v => v, () => null),
        todo.Tag.Match<string?>(t => t.Value, () => null),
        todo.CreatedByActorId,
        todo.CreatedAt);
}
