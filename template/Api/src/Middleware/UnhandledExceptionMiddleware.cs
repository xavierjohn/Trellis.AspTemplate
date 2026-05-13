namespace TodoSample.Api.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Catches unhandled exceptions and emits a generic <c>500 Internal Server Error</c>
/// problem-details response with the current trace id.
/// </summary>
/// <remarks>
/// This middleware is a <b>500 fallback only</b>. It does <b>not</b> map Trellis
/// <c>Result&lt;T&gt;</c> failures (<c>Error.NotFound</c>, <c>Error.UnprocessableContent</c>,
/// etc.) to HTTP status codes — that mapping is registered by
/// <c>builder.Services.AddTrellisAsp()</c> in <c>DependencyInjection.AddPresentation</c>.
/// </remarks>
internal class UnhandledExceptionMiddleware : IMiddleware
{
    private readonly ILogger<UnhandledExceptionMiddleware> _logger;

    public UnhandledExceptionMiddleware(ILogger<UnhandledExceptionMiddleware> logger) => _logger = logger;

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
        _logger.LogUnhandledExceptionMiddlewareMessage(exception);
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

