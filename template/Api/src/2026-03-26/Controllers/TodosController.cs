namespace TodoSample.Api.v2026_03_26.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using Trellis.ServiceLevelIndicators;
using TodoSample.Api.v2026_03_26.Models;
using TodoSample.Application.Todos;
using TodoSample.Domain;
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
    public ValueTask<ActionResult<TodoResponse>> Create(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken) =>
        _sender.Send(new CreateTodoCommand(request.Title, request.DueDate, request.Tag), cancellationToken)
            .ToHttpResponseAsync(
                TodoResponse.From,
                opts => opts
                    .WithETag(t => t.ETag)
                    .CreatedAtAction(nameof(GetById), t => new Microsoft.AspNetCore.Routing.RouteValueDictionary { ["id"] = (Guid)t.Id }))
            .AsActionResultAsync<TodoResponse>();

    /// <summary>
    /// Get a todo item by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<TodoResponse>> GetById(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new GetTodoByIdQuery(id), cancellationToken)
            .ToHttpResponseAsync(TodoResponse.From, opts => opts.WithETag(t => t.ETag))
            .AsActionResultAsync<TodoResponse>();

    /// <summary>
    /// Get all overdue todo items.
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<TodoResponse>), StatusCodes.Status200OK)]
    public ValueTask<ActionResult<IReadOnlyList<TodoResponse>>> GetOverdue(
        CancellationToken cancellationToken) =>
        _sender.Send(new GetOverdueTodosQuery(), cancellationToken)
            .ToHttpResponseAsync<IReadOnlyList<TodoItem>, IReadOnlyList<TodoResponse>>(
                todos => todos.Select(TodoResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<TodoResponse>>();

    /// <summary>
    /// Update a todo item's title, due date, and tag.
    /// Supports RFC 9110 conditional requests via If-Match header.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    public async ValueTask<ActionResult<TodoResponse>> Update(
        [CustomerResourceId] TodoId id,
        [FromBody] UpdateTodoRequest request,
        CancellationToken cancellationToken)
    {
        var ifMatchETags = ETagHelper.ParseIfMatch(Request);
        return await UpdateTodoCommand.TryCreate(id, request.Title, request.DueDate, request.Tag, ifMatchETags)
            .BindAsync(command => _sender.Send(command, cancellationToken))
            .ToHttpResponseAsync(TodoResponse.From, opts => opts.WithETag(t => t.ETag))
            .AsActionResultAsync<TodoResponse>();
    }

    /// <summary>
    /// Complete a todo item. Only the creator can complete their own todo.
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<ActionResult<TodoResponse>> Complete(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new CompleteTodoCommand(id), cancellationToken)
            .ToHttpResponseAsync(TodoResponse.From, opts => opts.WithETag(t => t.ETag))
            .AsActionResultAsync<TodoResponse>();

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
    public ValueTask<Microsoft.AspNetCore.Http.IResult> Delete(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new DeleteTodoCommand(id), cancellationToken)
            .ToHttpResponseAsync();
}
