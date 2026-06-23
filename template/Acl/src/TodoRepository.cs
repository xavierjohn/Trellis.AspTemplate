namespace TodoSample.AntiCorruptionLayer;

using TodoSample.Application;
using TodoSample.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="ITodoRepository"/>.
/// <para>
/// Inherits <c>FindByIdAsync</c>, <c>QueryAsync</c>, <c>Add</c>, <c>Remove</c>, and
/// <c>RemoveByIdAsync</c> from <see cref="RepositoryBase{TAggregate, TId}"/> — handlers stage
/// changes here, and <c>TransactionalCommandBehavior</c> commits on handler success.
/// Only the custom keyset-pagination query lives in this class.
/// </para>
/// </summary>
internal class TodoRepository : RepositoryBase<TodoItem, TodoId>, ITodoRepository
{
    public TodoRepository(AppDbContext context) : base(context)
    {
    }

    // ToPageAsync owns the OrderBy(keySelector), cursor decode, the seek WHERE, the
    // Take(Applied + 1) over-fetch, and the Page slice — we supply a pre-filtered query
    // and the sort-key projection. The Id (a stable, unique PK) is the keyset key; a
    // malformed cursor surfaces as Error.InvalidInput ("cursor.malformed"), never throws.
    public Task<Result<Page<TodoItem>>> QueryPageAsync(
        Specification<TodoItem> specification,
        PageSize pageSize,
        Cursor? cursor,
        CancellationToken cancellationToken) =>
        DbSet.Where(specification)
            .ToPageAsync(pageSize, cursor, t => (Guid)t.Id, cancellationToken: cancellationToken);
}


