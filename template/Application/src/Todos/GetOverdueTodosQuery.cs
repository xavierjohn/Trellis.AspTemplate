namespace TodoSample.Application.Todos;

using Mediator;
using TodoSample.Domain;
using Trellis.Authorization;

/// <summary>
/// Gets all overdue todo items.
/// </summary>
public sealed record GetOverdueTodosQuery : IQuery<Result<IReadOnlyList<TodoItem>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosRead];
}

/// <summary>
/// Handler for GetOverdueTodosQuery.
/// </summary>
public sealed class GetOverdueTodosQueryHandler : IQueryHandler<GetOverdueTodosQuery, Result<IReadOnlyList<TodoItem>>>
{
    private readonly ITodoRepository _repository;
    private readonly TimeProvider _timeProvider;

    public GetOverdueTodosQueryHandler(ITodoRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Result<IReadOnlyList<TodoItem>>> Handle(GetOverdueTodosQuery query, CancellationToken cancellationToken) =>
        Result.Success<IReadOnlyList<TodoItem>>(
            await _repository.GetAllAsync(new OverdueTodoSpecification(_timeProvider.GetUtcNow().UtcDateTime), cancellationToken));
}
