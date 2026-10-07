using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Middleware;
using McpTestServer.API.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;
using Xunit;

namespace McpTestServer.API.Tests.Middleware;

public sealed class LegacyHostRedirectMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_redirects_legacy_host_requests_to_canonical_origin()
    {
        var middleware = CreateMiddleware(
            new LegacyHostRedirectOptions
            {
                MCP_TEST_SERVER_CANONICAL_ORIGIN = "https://mcp-test-kitchen.cakewalk.security",
                MCP_TEST_SERVER_LEGACY_HOSTS = "mcp-test-server-cakewalk.fly.dev",
            });

        var httpContext = CreateHttpContext(
            host: "mcp-test-server-cakewalk.fly.dev",
            path: "/console",
            query: "?tab=scenarios");

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status308PermanentRedirect);
        httpContext.Response.Headers.Location.ToString()
            .Should()
            .Be("https://mcp-test-kitchen.cakewalk.security/console?tab=scenarios");
    }

    [Fact]
    public async Task InvokeAsync_does_not_redirect_canonical_host_requests()
    {
        var invoked = false;
        var middleware = CreateMiddleware(
            new LegacyHostRedirectOptions
            {
                MCP_TEST_SERVER_CANONICAL_ORIGIN = "https://mcp-test-kitchen.cakewalk.security",
                MCP_TEST_SERVER_LEGACY_HOSTS = "mcp-test-server-cakewalk.fly.dev",
            },
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

        var httpContext = CreateHttpContext(
            host: "mcp-test-kitchen.cakewalk.security",
            path: "/console");

        await middleware.InvokeAsync(httpContext);

        invoked.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_does_not_redirect_health_checks_on_legacy_host()
    {
        var invoked = false;
        var middleware = CreateMiddleware(
            new LegacyHostRedirectOptions
            {
                MCP_TEST_SERVER_CANONICAL_ORIGIN = "https://mcp-test-kitchen.cakewalk.security",
                MCP_TEST_SERVER_LEGACY_HOSTS = "mcp-test-server-cakewalk.fly.dev",
            },
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

        var httpContext = CreateHttpContext(
            host: "mcp-test-server-cakewalk.fly.dev",
            path: RequestLoggingConstants.HealthPath);

        await middleware.InvokeAsync(httpContext);

        invoked.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_is_inactive_when_redirect_options_are_not_configured()
    {
        var invoked = false;
        var middleware = CreateMiddleware(
            new LegacyHostRedirectOptions(),
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

        var httpContext = CreateHttpContext(
            host: "mcp-test-server-cakewalk.fly.dev",
            path: "/");

        await middleware.InvokeAsync(httpContext);

        invoked.Should().BeTrue();
    }

    private static LegacyHostRedirectMiddleware CreateMiddleware(
        LegacyHostRedirectOptions options,
        RequestDelegate? next = null) =>
        new(
            next ?? (_ => Task.CompletedTask),
            OptionsFactory.Create(options));

    private static DefaultHttpContext CreateHttpContext(string host, string path, string query = "")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.Path = path;
        httpContext.Request.QueryString = new QueryString(query);
        return httpContext;
    }
}
