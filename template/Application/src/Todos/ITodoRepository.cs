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

    /// <summary>
    /// Returns a single page of todos ordered by Id, with an optional opaque cursor.
    /// Returns <see cref="Error.BadRequest"/> when the cursor token cannot be decoded.
    /// </summary>
    Task<Result<Page<TodoItem>>> GetPageAsync(int requestedLimit, Cursor? cursor, CancellationToken cancellationToken);

    /// <summary>Saves a new or updated todo.</summary>
    Task<Result> SaveAsync(TodoItem todo, CancellationToken cancellationToken);

    /// <summary>Deletes a todo by ID.</summary>
    Task<Result> DeleteAsync(TodoId id, CancellationToken cancellationToken);
}
