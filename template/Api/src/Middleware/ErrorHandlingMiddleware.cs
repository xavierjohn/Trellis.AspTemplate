namespace TodoSample.Api.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Class Error handling Middleware.
/// </summary>
internal class ErrorHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogErrorHandlingMiddlewareMessage(exception);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // if you know you're in MVC-land, you can fall back to ProblemDetailsFactory
        if (context.RequestServices.GetService<IProblemDetailsService>() is not { } problem)
        {
            return;
        }

        ProblemDetailsContext ctx = new()
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred in our API. Please refer the trace id with our support team.",
            }
        };
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        ctx.ProblemDetails.Extensions["traceId"] = traceId;

        await problem.TryWriteAsync(ctx);
    }
}

