namespace BestWeatherForecast.Api.v2026_03_26.Controllers;

using BestWeatherForecast.Api.v2026_03_26.Models;
using BestWeatherForecast.Application.Todos;
using BestWeatherForecast.Domain;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using ServiceLevelIndicators;
using Trellis.Asp;

/// <summary>
/// Todo items controller.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TodosController(ISender sender) => _sender = sender;

    /// <summary>
    /// Create a new todo item.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async ValueTask<ActionResult<TodoResponse>> Create(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken) =>
        await _sender.Send(
            new CreateTodoCommand(request.Title, request.DueDate, request.Tag),
            cancellationToken)
            .MapAsync(TodoResponse.From)
            .ToCreatedAtActionResultAsync(this, nameof(GetById), r => new { id = r.Id });

    /// <summary>
    /// Get a todo item by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<TodoResponse>> GetById(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new GetTodoByIdQuery(id), cancellationToken)
            .ToActionResultAsync(this, TodoResponse.From);

    /// <summary>
    /// Get all overdue todo items.
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<TodoResponse>), StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<IReadOnlyList<TodoResponse>>> GetOverdue(
        CancellationToken cancellationToken) =>
        await _sender.Send(new GetOverdueTodosQuery(), cancellationToken)
            .ToActionResultAsync(this, todos => (IReadOnlyList<TodoResponse>)todos.Select(TodoResponse.From).ToList());

    /// <summary>
    /// Complete a todo item. Only the creator can complete their own todo.
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<TodoResponse>> Complete(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new CompleteTodoCommand(id), cancellationToken)
            .ToActionResultAsync(this, TodoResponse.From);

    /// <summary>
    /// This method throws to show the error handling middleware handles it.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    [HttpGet("throw")]
    public string Throw()
    {
        throw new NotImplementedException("Catch me middleware.");
    }

    /// <summary>
    /// Delete a todo item.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<Trellis.Unit>> Delete(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        await _sender.Send(new DeleteTodoCommand(id), cancellationToken)
            .ToActionResultAsync(this);
}
