namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Gets a single todo item by ID.
/// </summary>
public sealed record GetTodoByIdQuery(TodoId TodoId) : IQuery<Result<TodoItem>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosRead];
}

/// <summary>
/// Handler for GetTodoByIdQuery.
/// </summary>
public sealed class GetTodoByIdQueryHandler : IQueryHandler<GetTodoByIdQuery, Result<TodoItem>>
{
    private readonly ITodoRepository _repository;

    public GetTodoByIdQueryHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<TodoItem>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken) =>
        await _repository.FindByIdAsync(query.TodoId, cancellationToken)
            .ToResultAsync(new Error.NotFound(ResourceRef.For<TodoItem>(query.TodoId)) { Detail = $"Todo {query.TodoId} not found." });
}
