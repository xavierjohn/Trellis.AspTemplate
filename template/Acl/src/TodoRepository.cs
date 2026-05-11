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
        var query = DbSet
            .Where(specification)
            .OrderBy(t => t.Id);

        if (afterId is not null)
            query = (IOrderedQueryable<TodoItem>)query.Where(t => ((Guid)t.Id) > ((Guid)afterId));

        // Peek one extra to detect a next page without a separate count query.
        var rows = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasNext = rows.Count > limit;
        var items = hasNext ? rows.Take(limit).ToList() : rows;
        return (items, hasNext);
    }
}


