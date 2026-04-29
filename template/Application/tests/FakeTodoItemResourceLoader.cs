namespace Application.Tests;

using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.Testing;

/// <summary>
/// Shared resource loader for TodoItem authorization in tests.
/// </summary>
internal sealed class FakeTodoItemResourceLoader : SharedResourceLoaderById<TodoItem, TodoId>
{
    private readonly FakeRepository<TodoItem, TodoId> _repo;

    public FakeTodoItemResourceLoader(FakeRepository<TodoItem, TodoId> repo) => _repo = repo;

    public override Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        _repo.GetByIdAsync(id, cancellationToken);
}
