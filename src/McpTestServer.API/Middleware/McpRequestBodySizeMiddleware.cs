using McpTestServer.API.Constants;

namespace McpTestServer.API.Middleware;

public sealed class McpRequestBodySizeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(McpPaths.McpEndpoint)
            && context.Request.ContentLength > McpObservationConstants.MaxRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        await next(context);
    }
}
