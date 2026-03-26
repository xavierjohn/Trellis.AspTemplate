namespace TodoSample.Application;

using TodoSample.Domain;

/// <summary>
/// Repository interface for TodoItem persistence.
/// </summary>
public interface ITodoRepository
{
    /// <summary>Finds a todo by ID. Returns Maybe.None if not found.</summary>
    Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken);

    /// <summary>Gets all todos matching the specification.</summary>
    Task<IReadOnlyList<TodoItem>> GetAllAsync(Specification<TodoItem> specification, CancellationToken cancellationToken);

    /// <summary>Saves a new or updated todo.</summary>
    Task<Result<Unit>> SaveAsync(TodoItem todo, CancellationToken cancellationToken);

    /// <summary>Deletes a todo by ID.</summary>
    Task<Result<Unit>> DeleteAsync(TodoId id, CancellationToken cancellationToken);
}
