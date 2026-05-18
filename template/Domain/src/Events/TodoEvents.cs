namespace TodoSample.Domain;

using Trellis.Authorization;

/// <summary>
/// Raised when a new todo item is created.
/// </summary>
public sealed record TodoCreated(TodoId TodoId, Title Title, ActorId CreatedByActorId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Raised when a todo item is completed.
/// </summary>
public sealed record TodoCompleted(TodoId TodoId, DateTimeOffset OccurredAt) : IDomainEvent;
