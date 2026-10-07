using McpTestServer.API.Constants;
using McpTestServer.API.Options;
using McpTestServer.API.Services.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Middleware;

public sealed class McpPatAuthMiddleware(
    RequestDelegate next,
    IOptions<McpPatOptions> options,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context, IUserMcpPatValidator patValidator)
    {
        if (!context.Request.Path.StartsWithSegments(McpPaths.McpEndpoint))
        {
            await next(context);
            return;
        }

        var expectedToken = options.Value.MCP_PAT;
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(expectedToken) && !authHeader.StartsWith(McpPatConstants.BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (environment.IsDevelopment())
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!authHeader.StartsWith(McpPatConstants.BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var providedToken = authHeader[McpPatConstants.BearerPrefix.Length..];
        var validation = await patValidator.ValidateAsync(providedToken, context.RequestAborted);

        if (!validation.IsValid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!string.IsNullOrEmpty(validation.CallerEmail))
        {
            context.Items[McpCallerConstants.CallerEmailItemKey] = validation.CallerEmail;
        }

        await next(context);
    }
}
