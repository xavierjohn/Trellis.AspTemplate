namespace Domain.Tests;

using TodoSample.Domain;

#pragma warning disable TRLS003 // Tests assert success before accessing .Value

public class OverdueTodoSpecificationTests
{
    private static Title TestTitle => Title.Create("Test todo");

    [Fact]
    public void Matches_active_past_due_todo()
    {
        var pastDue = DueDate.Create(DateTime.UtcNow.AddDays(-3));
        var result = TodoItem.TryCreate(TestTitle, pastDue, Maybe<Tag>.None, "actor-1");
        result.Should().BeSuccess();
        result.Value.Start().Should().BeSuccess();
        var spec = new OverdueTodoSpecification(DateTime.UtcNow);

        spec.IsSatisfiedBy(result.Value).Should().BeTrue();
    }

    [Fact]
    public void Does_not_match_active_future_due_todo()
    {
        var futureDue = DueDate.Create(DateTime.UtcNow.AddDays(7));
        var result = TodoItem.TryCreate(TestTitle, futureDue, Maybe<Tag>.None, "actor-1");
        result.Should().BeSuccess();
        result.Value.Start().Should().BeSuccess();
        var spec = new OverdueTodoSpecification(DateTime.UtcNow);

        spec.IsSatisfiedBy(result.Value).Should().BeFalse();
    }

    [Fact]
    public void Does_not_match_pending_past_due_todo()
    {
        var pastDue = DueDate.Create(DateTime.UtcNow.AddDays(-3));
        var result = TodoItem.TryCreate(TestTitle, pastDue, Maybe<Tag>.None, "actor-1");
        result.Should().BeSuccess();
        var spec = new OverdueTodoSpecification(DateTime.UtcNow);

        spec.IsSatisfiedBy(result.Value).Should().BeFalse();
    }

    [Fact]
    public void And_composition_matches_overdue_with_tag()
    {
        var pastDue = DueDate.Create(DateTime.UtcNow.AddDays(-3));
        var tag = Tag.Create("work");
        var result = TodoItem.TryCreate(TestTitle, pastDue, Maybe.From(tag), "actor-1");
        result.Should().BeSuccess();
        result.Value.Start().Should().BeSuccess();

        var overdueSpec = new OverdueTodoSpecification(DateTime.UtcNow);
        var hasTagSpec = new HasTagSpecification();
        var combined = overdueSpec.And(hasTagSpec);

        combined.IsSatisfiedBy(result.Value).Should().BeTrue();
    }

    [Fact]
    public void And_composition_rejects_overdue_without_tag()
    {
        var pastDue = DueDate.Create(DateTime.UtcNow.AddDays(-3));
        var result = TodoItem.TryCreate(TestTitle, pastDue, Maybe<Tag>.None, "actor-1");
        result.Should().BeSuccess();
        result.Value.Start().Should().BeSuccess();

        var overdueSpec = new OverdueTodoSpecification(DateTime.UtcNow);
        var hasTagSpec = new HasTagSpecification();
        var combined = overdueSpec.And(hasTagSpec);

        combined.IsSatisfiedBy(result.Value).Should().BeFalse();
    }
}

/// <summary>
/// Test specification: matches todos that have a tag assigned.
/// </summary>
file class HasTagSpecification : Specification<TodoItem>
{
    public override System.Linq.Expressions.Expression<Func<TodoItem, bool>> ToExpression() =>
        todo => todo.Tag.HasValue;
}
