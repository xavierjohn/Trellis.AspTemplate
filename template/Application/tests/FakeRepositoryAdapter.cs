namespace Application.Tests;

using BestWeatherForecast.Application;
using BestWeatherForecast.Domain;
using Trellis.Testing.Fakes;

/// <summary>
/// Adapts FakeRepository to the ITodoRepository interface.
/// </summary>
internal class FakeRepositoryAdapter : ITodoRepository
{
    private readonly FakeRepository<TodoItem, TodoId> _repo;

    public FakeRepositoryAdapter(FakeRepository<TodoItem, TodoId> repo) => _repo = repo;

    public Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        _repo.GetByIdAsync(id, cancellationToken);

    public Task<Result<IReadOnlyList<TodoItem>>> GetAllAsync(Specification<TodoItem> specification, CancellationToken cancellationToken)
    {
        var items = _repo.GetAll().Where(specification.IsSatisfiedBy).ToList();
        return Task.FromResult(Result.Success<IReadOnlyList<TodoItem>>(items));
    }

    public async Task<Result<Unit>> SaveAsync(TodoItem todo, CancellationToken cancellationToken)
    {
        var result = await _repo.SaveAsync(todo, cancellationToken);
        return result.Map(_ => default(Unit));
    }

    public async Task<Result<Unit>> DeleteAsync(TodoId id, CancellationToken cancellationToken)
    {
        var result = await _repo.DeleteAsync(id, cancellationToken);
        return result.Map(_ => default(Unit));
    }
}
