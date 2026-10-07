using McpTestServer.API.Constants;

namespace McpTestServer.API.Middleware;

public sealed class UnauthenticatedRootRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path == "/"
            && context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.Redirect(LoginPaths.Page);
            return;
        }

        await next(context);
    }
}
