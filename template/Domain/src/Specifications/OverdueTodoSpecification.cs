namespace TodoSample.Domain;

using System.Linq.Expressions;

/// <summary>
/// Matches todo items that are overdue: status is Active and due date is in the past.
/// </summary>
public class OverdueTodoSpecification : Specification<TodoItem>
{
    private readonly DateTime _asOf;

    /// <summary>
    /// Creates a specification that checks for overdue todos relative to the given date.
    /// </summary>
    public OverdueTodoSpecification(DateTime asOf) => _asOf = asOf;

    /// <inheritdoc />
    public override Expression<Func<TodoItem, bool>> ToExpression() =>
        todo => todo.Status == TodoStatus.Active && todo.DueDate < _asOf;
}
