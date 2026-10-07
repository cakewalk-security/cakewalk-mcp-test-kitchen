using McpTestServer.API.Constants;
using McpTestServer.API.Options;

namespace McpTestServer.API.Middleware;

public sealed class OAuthPublicOriginMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsOAuthCallback(context.Request.Path))
        {
            ApplyPublicOriginToRequest(context.Request);
        }

        await next(context);
    }

    private static bool IsOAuthCallback(PathString path) =>
        path.Equals(OAuthCallbackPaths.GoogleSignIn, StringComparison.OrdinalIgnoreCase)
        || path.Equals(OAuthCallbackPaths.GitHubSignIn, StringComparison.OrdinalIgnoreCase);

    private void ApplyPublicOriginToRequest(HttpRequest request)
    {
        var publicOrigin = configuration[OAuthPublicOriginOptions.EnvKey];
        if (string.IsNullOrWhiteSpace(publicOrigin)
            || !Uri.TryCreate(publicOrigin.TrimEnd('/'), UriKind.Absolute, out var origin))
        {
            return;
        }

        request.Scheme = origin.Scheme;
        request.Host = new HostString(origin.Authority);
    }
}
