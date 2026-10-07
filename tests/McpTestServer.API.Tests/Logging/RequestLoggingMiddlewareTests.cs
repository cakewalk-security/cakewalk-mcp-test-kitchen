using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace McpTestServer.API.Tests.Logging;

public sealed class RequestLoggingMiddlewareTests
{
    [Theory]
    [InlineData("GET", "/mcp", 200, 2, null)]
    [InlineData("HEAD", "/mcp", 200, 2, null)]
    [InlineData("OPTIONS", "/mcp", 204, 1, null)]
    [InlineData("GET", "/api/management/me", 200, 5, null)]
    [InlineData("GET", "/api/health", 200, 1, null)]
    [InlineData("GET", "/assets/index.js", 200, 1, null)]
    [InlineData("POST", "/mcp", 200, 45, LogLevel.Information)]
    [InlineData("PUT", "/api/management/me/scenario", 200, 20, LogLevel.Information)]
    [InlineData("GET", "/mcp", 200, RequestLoggingConstants.SlowRequestThresholdMs, LogLevel.Information)]
    [InlineData("GET", "/mcp", 401, 2, LogLevel.Warning)]
    [InlineData("POST", "/mcp", 429, 3, LogLevel.Warning)]
    [InlineData("GET", "/api/health", 503, 4, LogLevel.Error)]
    [InlineData("GET", "/mcp", 500, 10, LogLevel.Error)]
    public void GetCompletionLogLevel_matches_noise_policy(
        string method,
        string path,
        int statusCode,
        long elapsedMs,
        LogLevel? expected)
    {
        RequestLoggingMiddleware.GetCompletionLogLevel(method, path, statusCode, elapsedMs)
            .Should()
            .Be(expected);
    }

    [Fact]
    public async Task InvokeAsync_logs_post_method_path_status_and_duration()
    {
        var logger = new CollectingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            },
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = McpPaths.McpEndpoint;
        httpContext.TraceIdentifier = "trace-1";

        await middleware.InvokeAsync(httpContext);

        logger.Entries.Should().ContainSingle();
        var entry = logger.Entries[0];
        entry.Level.Should().Be(LogLevel.Information);
        entry.Properties["RequestMethod"].Should().Be(HttpMethods.Post);
        entry.Properties["RequestPath"].Should().Be(McpPaths.McpEndpoint);
        entry.Properties["StatusCode"].Should().Be(StatusCodes.Status201Created);
        entry.Properties.Should().ContainKey("ElapsedMs");
    }

    [Fact]
    public async Task InvokeAsync_does_not_log_successful_get_mcp()
    {
        var logger = new CollectingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = McpPaths.McpEndpoint;

        await middleware.InvokeAsync(httpContext);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_does_not_log_successful_health_probes()
    {
        var logger = new CollectingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = RequestLoggingConstants.HealthPath;

        await middleware.InvokeAsync(httpContext);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_logs_failed_get_mcp_as_warning()
    {
        var logger = new CollectingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = McpPaths.McpEndpoint;

        await middleware.InvokeAsync(httpContext);

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].Level.Should().Be(LogLevel.Warning);
        logger.Entries[0].Properties["StatusCode"].Should().Be(StatusCodes.Status401Unauthorized);
    }

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<CollectedLog> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object>> pairs
                ? pairs.ToDictionary(pair => pair.Key, pair => pair.Value)
                : [];

            Entries.Add(new CollectedLog(logLevel, formatter(state, exception), properties));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed record CollectedLog(
        LogLevel Level,
        string Message,
        Dictionary<string, object> Properties);
}
