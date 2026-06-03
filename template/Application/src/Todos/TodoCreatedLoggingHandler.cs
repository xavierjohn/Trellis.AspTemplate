namespace TodoSample.Application.Todos;

using Microsoft.Extensions.Logging;
using TodoSample.Domain;
using Trellis.Mediator;

internal sealed partial class TodoCreatedLoggingHandler(ILogger<TodoCreatedLoggingHandler> logger) : IDomainEventHandler<TodoCreated>
{
    public ValueTask HandleAsync(TodoCreated domainEvent, CancellationToken cancellationToken)
    {
        LogTodoCreated(
            logger,
            domainEvent.TodoId.Value,
            domainEvent.CreatedByActorId,
            domainEvent.OccurredAt,
            domainEvent.Title.Value);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(1, LogLevel.Information, "Todo {TodoId} created by {ActorId} at {OccurredAt} with title {Title}")]
    private static partial void LogTodoCreated(
        ILogger logger,
        Guid todoId,
        string actorId,
        DateTimeOffset occurredAt,
        string title);
}
