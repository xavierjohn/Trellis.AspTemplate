namespace Application.Tests;

using TodoSample.Application;
using TodoSample.Domain;
using Trellis.Testing;

/// <summary>
/// Adapts FakeRepository to the ITodoRepository interface.
/// </summary>
internal class FakeRepositoryAdapter : ITodoRepository
{
    private readonly FakeRepository<TodoItem, TodoId> _repo;

    public FakeRepositoryAdapter(FakeRepository<TodoItem, TodoId> repo) => _repo = repo;

    public async Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken)
    {
        var result = await _repo.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Maybe.From(result.Unwrap()) : Maybe<TodoItem>.None;
    }

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(Specification<TodoItem> specification, CancellationToken cancellationToken)
    {
        var items = _repo.GetAll().Where(specification.IsSatisfiedBy).ToList();
        return Task.FromResult<IReadOnlyList<TodoItem>>(items);
    }

    public async Task<Result> SaveAsync(TodoItem todo, CancellationToken cancellationToken) =>
        await _repo.SaveAsync(todo, cancellationToken);

    public async Task<Result> DeleteAsync(TodoId id, CancellationToken cancellationToken) =>
        await _repo.DeleteAsync(id, cancellationToken);
}
