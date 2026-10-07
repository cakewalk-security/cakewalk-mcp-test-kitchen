using System.Diagnostics;
using McpTestServer.API.Constants;

namespace McpTestServer.API.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            var level = GetCompletionLogLevel(method, path, context.Response.StatusCode, elapsedMs);
            if (level is { } logLevel)
            {
                logger.Log(
                    logLevel,
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs}ms",
                    method,
                    path,
                    context.Response.StatusCode,
                    elapsedMs);
            }
        }
    }

    internal static LogLevel? GetCompletionLogLevel(
        string method,
        string path,
        int statusCode,
        long elapsedMs)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Error;
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            return LogLevel.Warning;
        }

        if (IsOmittedPath(path))
        {
            return null;
        }

        if (elapsedMs >= RequestLoggingConstants.SlowRequestThresholdMs)
        {
            return LogLevel.Information;
        }

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            return null;
        }

        return LogLevel.Information;
    }

    private static bool IsOmittedPath(string path)
    {
        if (path.Equals(RequestLoggingConstants.HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.StartsWith(RequestLoggingConstants.AssetsPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
