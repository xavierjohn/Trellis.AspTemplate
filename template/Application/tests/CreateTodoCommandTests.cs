namespace Application.Tests;

using BestWeatherForecast.Application.Todos;
using BestWeatherForecast.Domain;
using Mediator;
using Trellis.Testing.Fakes;

#pragma warning disable TRLS003 // Tests assert success before accessing .Value

public class CreateTodoCommandTests
{
    private readonly ISender _sender;
    private readonly FakeRepository<TodoItem, TodoId> _repo;

    public CreateTodoCommandTests(ISender sender, FakeRepository<TodoItem, TodoId> repo)
    {
        _sender = sender;
        _repo = repo;
    }

    [Fact]
    public async Task Create_valid_todo_returns_success()
    {
        var command = new CreateTodoCommand(
            Title.Create("Buy groceries"),
            DueDate.Create(DateTime.UtcNow.AddDays(7)),
            Maybe<Tag>.None);

        var result = await _sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Value.Title.Should().Be("Buy groceries");
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Create_todo_with_tag_preserves_tag()
    {
        var tag = Tag.Create("work");
        var command = new CreateTodoCommand(
            Title.Create("Finish report"),
            DueDate.Create(DateTime.UtcNow.AddDays(3)),
            Maybe.From(tag));

        var result = await _sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        result.Value.Tag.Should().Be("work");
    }

    [Fact]
    public async Task Create_todo_is_persisted_in_repository()
    {
        var command = new CreateTodoCommand(
            Title.Create("Persisted todo"),
            DueDate.Create(DateTime.UtcNow.AddDays(5)),
            Maybe<Tag>.None);

        var result = await _sender.Send(command, TestContext.Current.CancellationToken);

        result.Should().BeSuccess();
        var todoId = TodoId.TryCreate(result.Value.Id);
        todoId.Should().BeSuccess();
        var stored = await _repo.GetByIdAsync(todoId.Value, TestContext.Current.CancellationToken);
        stored.Should().BeSuccess();
    }
}
