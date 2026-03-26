namespace BestWeatherForecast.Application.Todos;

using BestWeatherForecast.Domain;
using Mediator;
using Trellis.Authorization;

/// <summary>
/// Gets all overdue todo items.
/// </summary>
public sealed record GetOverdueTodosQuery : IQuery<Result<IReadOnlyList<TodoDto>>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosRead];
}

/// <summary>
/// Handler for GetOverdueTodosQuery.
/// </summary>
public sealed class GetOverdueTodosQueryHandler : IQueryHandler<GetOverdueTodosQuery, Result<IReadOnlyList<TodoDto>>>
{
    private readonly ITodoRepository _repository;

    public GetOverdueTodosQueryHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<IReadOnlyList<TodoDto>>> Handle(GetOverdueTodosQuery query, CancellationToken cancellationToken) =>
        await _repository.GetAllAsync(new OverdueTodoSpecification(DateTime.UtcNow), cancellationToken)
            .MapAsync(todos => (IReadOnlyList<TodoDto>)todos.Select(TodoDto.From).ToList());
}
