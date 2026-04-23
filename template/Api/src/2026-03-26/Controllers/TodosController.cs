namespace TodoSample.Api.v2026_03_26.Controllers;

using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
    /// List todo items as a single cursor-paginated page. The framework emits an RFC 8288
    /// <c>Link</c> header with <c>rel="next"</c> when more pages are available, and the
    /// response body is the <c>PagedResponse&lt;TodoResponse&gt;</c> envelope.
    /// </summary>
    [HttpGet(Name = "Todos_List")]
    [ProducesResponseType(typeof(PagedResponse<TodoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ValueTask<ActionResult<PagedResponse<TodoResponse>>> List(
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromServices] LinkGenerator links,
        CancellationToken cancellationToken) =>
        _sender.Send(new GetTodosQuery(limit, cursor), cancellationToken)
            .ToHttpResponseAsync(
                nextUrlBuilder: (c, applied) =>
                    links.GetUriByName(HttpContext, "Todos_List",
                        values: new Microsoft.AspNetCore.Routing.RouteValueDictionary
                        {
                            ["limit"] = applied,
                            ["cursor"] = c.Token,
                            ["api-version"] = "2026-03-26",
                        })
                    ?? throw new InvalidOperationException("Route 'Todos_List' not registered."),
                body: TodoResponse.From)
            .AsActionResultAsync<PagedResponse<TodoResponse>>();

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
    /// Idempotent — when the new values match the current state, returns 204 No Content
    /// without rewriting; otherwise returns 200 with the updated representation.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public ValueTask<ActionResult<TodoResponse>> Update(
        [CustomerResourceId] TodoId id,
        [FromBody] UpdateTodoRequest request,
        CancellationToken cancellationToken)
    {
        var ifMatchETags = ETagHelper.ParseIfMatch(Request);
        return _sender.Send(new UpdateTodoCommand(id, request.Title, request.DueDate, request.Tag, ifMatchETags), cancellationToken)
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
