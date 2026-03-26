namespace BestWeatherForecast.Application.Todos;

using BestWeatherForecast.Domain;
using Mediator;
using Trellis.Authorization;

/// <summary>
/// Gets a single todo item by ID.
/// </summary>
public sealed record GetTodoByIdQuery(TodoId TodoId) : IQuery<Result<TodoDto>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.TodosRead];
}

/// <summary>
/// Handler for GetTodoByIdQuery.
/// </summary>
public sealed class GetTodoByIdQueryHandler : IQueryHandler<GetTodoByIdQuery, Result<TodoDto>>
{
    private readonly ITodoRepository _repository;

    public GetTodoByIdQueryHandler(ITodoRepository repository) => _repository = repository;

    public async ValueTask<Result<TodoDto>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken) =>
        await _repository.GetByIdAsync(query.TodoId, cancellationToken)
            .MapAsync(TodoDto.From);
}
