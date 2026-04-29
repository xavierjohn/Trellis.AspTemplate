namespace TodoSample.Api.v2026_03_26.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using Trellis;
using Trellis.Asp;
using Trellis.ServiceLevelIndicators;
using TodoSample.Api.v2026_03_26.Models;
using TodoSample.Application.Todos;
using TodoSample.Domain;

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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<TodoResponse>> Create(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken) =>
        _sender.Send(new CreateTodoCommand(request.Title, request.DueDate, request.Tag), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(
                TodoResponse.From,
                opts => opts
                    .CreatedAtRoute("Todos_GetById", t => new Microsoft.AspNetCore.Routing.RouteValueDictionary
                    {
                        ["id"] = (Guid)t.Id,
                        ["api-version"] = "2026-03-26",
                    })
                    .WithETag(t => EntityTagValue.Strong(t.ETag)))
            .AsActionResultAsync<TodoResponse>();

    /// <summary>
    /// Get a todo item by ID.
    /// </summary>
    [HttpGet("{id}", Name = "Todos_GetById")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TodoResponse>> GetById(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new GetTodoByIdQuery(id), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(
                TodoResponse.From,
                opts => opts.WithETag(t => EntityTagValue.Strong(t.ETag)).EvaluatePreconditions())
            .AsActionResultAsync<TodoResponse>();

    /// <summary>
    /// Get all overdue todo items.
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<TodoResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<TodoResponse>>> GetOverdue(
        CancellationToken cancellationToken) =>
        _sender.Send(new GetOverdueTodosQuery(), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(todos => (IReadOnlyList<TodoResponse>)todos.Select(TodoResponse.From).ToList())
            .AsActionResultAsync<IReadOnlyList<TodoResponse>>();

    /// <summary>
    /// Update a todo item's title, due date, and tag.
    /// Supports RFC 9110 conditional requests via If-Match header.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<TodoResponse>> Update(
        [CustomerResourceId] TodoId id,
        [FromBody] UpdateTodoRequest request,
        CancellationToken cancellationToken)
    {
        var ifMatchETags = ETagHelper.ParseIfMatch(Request);
        return UpdateTodoCommand.TryCreate(id, request.Title, request.DueDate, request.Tag, ifMatchETags)
            .BindAsync(command => _sender.Send(command, cancellationToken).AsTask())
            .ToHttpResponseAsync(
                TodoResponse.From,
                opts => opts.WithETag(t => EntityTagValue.Strong(t.ETag)))
            .AsActionResultAsync<TodoResponse>();
    }

    /// <summary>
    /// Complete a todo item. Only the creator can complete their own todo.
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TodoResponse>> Complete(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new CompleteTodoCommand(id), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync(
                TodoResponse.From,
                opts => opts.WithETag(t => EntityTagValue.Strong(t.ETag)))
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
    public Task<Microsoft.AspNetCore.Http.IResult> Delete(
        [CustomerResourceId] TodoId id,
        CancellationToken cancellationToken) =>
        _sender.Send(new DeleteTodoCommand(id), cancellationToken)
            .AsTask()
            .ToHttpResponseAsync();
}
