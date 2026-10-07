using McpTestServer.API.Constants;
using McpTestServer.API.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Middleware;

public sealed class LegacyHostRedirectMiddleware(RequestDelegate next, IOptions<LegacyHostRedirectOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var redirectOptions = options.Value;
        if (!redirectOptions.IsEnabled)
        {
            await next(context);
            return;
        }

        var host = context.Request.Host.Host;
        if (!redirectOptions.LegacyHostSet.Contains(host))
        {
            await next(context);
            return;
        }

        if (IsExcludedPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        context.Response.Headers.Location = redirectOptions.BuildRedirectUrl(context.Request);
        context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
    }

    private static bool IsExcludedPath(PathString path) =>
        path.Equals(RequestLoggingConstants.HealthPath, StringComparison.OrdinalIgnoreCase);
}
