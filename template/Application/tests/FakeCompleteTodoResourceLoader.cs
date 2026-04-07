namespace Application.Tests;

using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Authorization;
using Trellis.Testing;

/// <summary>
/// Resource loader for CompleteTodoCommand — loads TodoItem by ID from the fake repository.
/// </summary>
internal class FakeCompleteTodoResourceLoader : ResourceLoaderById<CompleteTodoCommand, TodoItem, TodoId>
{
    private readonly FakeRepository<TodoItem, TodoId> _repo;

    public FakeCompleteTodoResourceLoader(FakeRepository<TodoItem, TodoId> repo) => _repo = repo;

    protected override TodoId GetId(CompleteTodoCommand message) => message.TodoId;

    protected override Task<Result<TodoItem>> GetByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        _repo.GetByIdAsync(id, cancellationToken);
}
