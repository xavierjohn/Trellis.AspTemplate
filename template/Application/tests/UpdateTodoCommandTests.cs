namespace Application.Tests;

using Microsoft.Extensions.Time.Testing;
using TodoSample.Application.Todos;
using TodoSample.Domain;

public class UpdateTodoCommandTests
{
    [Fact]
    public void TryCreate_future_due_date_succeeds()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero));

        var result = UpdateTodoCommand.TryCreate(
            TodoId.NewUniqueV7(),
            Title.Create("Test"),
            DueDate.Create(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
            Maybe<Tag>.None,
            timeProvider);

        result.Should().BeSuccess();
    }

    [Fact]
    public void TryCreate_past_due_date_fails()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 26, 12, 0, 0, TimeSpan.Zero));

        var result = UpdateTodoCommand.TryCreate(
            TodoId.NewUniqueV7(),
            Title.Create("Test"),
            DueDate.Create(new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc)),
            Maybe<Tag>.None,
            timeProvider);

        result.Should().BeFailure()
            .Which.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public void TryCreate_without_time_provider_uses_system_clock()
    {
        var result = UpdateTodoCommand.TryCreate(
            TodoId.NewUniqueV7(),
            Title.Create("Test"),
            DueDate.Create(DateTime.UtcNow.AddDays(7)),
            Maybe<Tag>.None);

        result.Should().BeSuccess();
    }
}
