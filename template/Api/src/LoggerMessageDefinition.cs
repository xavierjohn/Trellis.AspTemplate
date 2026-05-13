namespace TodoSample.Api;

internal static partial class LoggerMessageDefinition
{
    private static readonly Action<ILogger, string, Exception?> UnhandledExceptionMiddlewareLogMessageDefinition =
        LoggerMessage.Define<string>(LogLevel.Error, 0, "Unhandled exception caught by fallback middleware. {ExceptionToString}");

    public static void LogUnhandledExceptionMiddlewareMessage(this ILogger logger, Exception exception)
        => UnhandledExceptionMiddlewareLogMessageDefinition(logger, exception.ToString(), exception);
}
