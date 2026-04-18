namespace Application.Tests;

using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.Testing;

/// <summary>
/// Shared resource loader for TodoItem in tests — loads by ID from the fake repository.
/// </summary>
internal sealed class FakeTodoItemResourceLoader(FakeRepository<TodoItem, TodoId> repo)
    : SharedResourceLoaderById<TodoItem, TodoId>
{
    public override Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        repo.GetByIdAsync(id, cancellationToken);
}
