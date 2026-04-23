namespace Application.Tests;

using Mediator;
using Microsoft.Extensions.Time.Testing;
using TodoSample.Application.Todos;
using TodoSample.Domain;
using Trellis.Testing;

public class UpdateTodoCommandTests
{
    private readonly ISender _sender;
    private readonly FakeRepository<TodoItem, TodoId> _repo;
    private readonly TestActorProvider _actorProvider;

    public UpdateTodoCommandTests(ISender sender, FakeRepository<TodoItem, TodoId> repo, TestActorProvider actorProvider)
    {
        _sender = sender;
        _repo = repo;
        _actorProvider = actorProvider;
    }

    [Fact]
    public async Task Update_with_changed_values_returns_Updated_outcome()
    {
        var created = await SeedAsync("Original", DateTime.UtcNow.AddDays(5));

        var newDueDate = DateTime.UtcNow.AddDays(14);
        var result = await _sender.Send(
            new UpdateTodoCommand(created.Id, Title.Create("Renamed"), DueDate.Create(newDueDate), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        var outcome = result.Unwrap();
        outcome.Should().BeOfType<WriteOutcome<TodoItem>.Updated>();
        ((WriteOutcome<TodoItem>.Updated)outcome).Value.Title.Should().Be(Title.Create("Renamed"));
    }

    [Fact]
    public async Task Update_with_same_values_returns_UpdatedNoContent_outcome()
    {
        var dueDate = DateTime.UtcNow.AddDays(5);
        var created = await SeedAsync("Same", dueDate);

        var result = await _sender.Send(
            new UpdateTodoCommand(created.Id, created.Title, created.DueDate, created.Tag),
            TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Unwrap().Should().BeOfType<WriteOutcome<TodoItem>.UpdatedNoContent>();
    }

    [Fact]
    public async Task Update_with_past_due_date_fails_validation()
    {
        // The validator runs before the handler and short-circuits to UnprocessableContent.
        var created = await SeedAsync("Will fail validation", DateTime.UtcNow.AddDays(5));

        var result = await _sender.Send(
            new UpdateTodoCommand(created.Id, created.Title, DueDate.Create(DateTime.UtcNow.AddDays(-1)), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);

        result.Should().BeFailure()
            .Which.Should().BeOfType<Error.UnprocessableContent>();
    }

    [Fact]
    public async Task Update_unknown_id_returns_NotFound()
    {
        var result = await _sender.Send(
            new UpdateTodoCommand(TodoId.NewUniqueV7(), Title.Create("Anything"), DueDate.Create(DateTime.UtcNow.AddDays(3)), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);

        result.Should().BeFailureOfType<Error.NotFound>();
    }

    private async Task<TodoItem> SeedAsync(string title, DateTime dueDate)
    {
        var createResult = await _sender.Send(
            new CreateTodoCommand(Title.Create(title), DueDate.Create(dueDate), Maybe<Tag>.None),
            TestContext.Current.CancellationToken);
        createResult.Should().BeSuccess();
        return createResult.Unwrap();
    }
}
