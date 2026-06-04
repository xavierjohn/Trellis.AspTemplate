namespace TodoSample.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
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

    public async Task<(IReadOnlyList<TodoItem> Items, bool HasNext)> QueryPageAsync(
        Specification<TodoItem> specification,
        TodoId? afterId,
        int limit,
        CancellationToken cancellationToken)
    {
        // Apply filters BEFORE OrderBy: Queryable.Where returns IQueryable<T>, not
        // IOrderedQueryable<T>, so casting the result of Where back to IOrderedQueryable
        // after an earlier OrderBy is fragile and provider-dependent (LINQ-to-Objects
        // throws InvalidCastException). OrderBy must be the last clause in the keyset
        // chain so the IOrderedQueryable shape is preserved for .Take(limit + 1).
        var filtered = DbSet.Where(specification);
        if (afterId is not null)
            filtered = filtered.Where(t => ((Guid)t.Id) > ((Guid)afterId));

        var query = filtered.OrderBy(t => t.Id);

        // Peek one extra to detect a next page without a separate count query.
        var rows = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasNext = rows.Count > limit;
        var items = hasNext ? rows.Take(limit).ToList() : rows;
        return (items, hasNext);
    }
}


