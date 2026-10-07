using McpTestServer.API.Constants;
using McpTestServer.API.Middleware;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace McpTestServer.API.Extensions;

public static class MiddlewareCollectionExtensions
{
    public static WebApplication UseRequestLogging(this WebApplication app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }

    public static WebApplication UseProxyForwardedHeaders(this WebApplication app)
    {
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor
                              | ForwardedHeaders.XForwardedProto
                              | ForwardedHeaders.XForwardedHost,
        };

        if (app.Environment.IsDevelopment())
        {
            forwardedHeadersOptions.ForwardLimit = null;
            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
        }
        else if (app.Configuration.GetValue<bool>(McpTestServerHostEnv.TrustProxyForwardedFor))
        {
            // Take the client IP appended by the edge proxy so per-IP rate limits work.
            // Host and scheme are deliberately not taken from forwarded headers.
            forwardedHeadersOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            forwardedHeadersOptions.ForwardLimit = 1;
            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
        }
        else
        {
            forwardedHeadersOptions.ForwardLimit = 1;
        }

        app.UseForwardedHeaders(forwardedHeadersOptions);
        return app;
    }

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        var isDevelopment = app.Environment.IsDevelopment();
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            if (!isDevelopment)
            {
                headers.StrictTransportSecurity = SecurityHeaders.StrictTransportSecurity;
            }

            headers.XContentTypeOptions = SecurityHeaders.ContentTypeOptions;
            headers.XFrameOptions = SecurityHeaders.FrameOptions;
            headers.ContentSecurityPolicy = SecurityHeaders.ContentSecurityPolicy;
            headers["Referrer-Policy"] = SecurityHeaders.ReferrerPolicy;
            await next();
        });

        return app;
    }

    public static WebApplication UseMiddlewares(this WebApplication app)
    {
        app.UseMiddleware<LegacyHostRedirectMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseStaticFiles(CreateStaticFileOptions());

        app.UseRouting();
        app.UseMiddleware<OAuthPublicOriginMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UnauthenticatedRootRedirectMiddleware>();
        app.UseRateLimiter();

        app.MapControllers();
        app.MapMcp(McpPaths.McpEndpoint);

        app.MapFallbackToFile("index.html", CreateStaticFileOptions(isSpaFallback: true));

        return app;
    }

    private static StaticFileOptions CreateStaticFileOptions(bool isSpaFallback = false)
    {
        return new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (isSpaFallback)
                {
                    context.Context.Response.Headers.CacheControl = "no-cache";
                    return;
                }

                var path = context.Context.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                    return;
                }

                context.Context.Response.Headers.CacheControl = "public, max-age=86400";
            },
        };
    }

    public static WebApplication UseMcpPatAuth(this WebApplication app)
    {
        // Rate limit and authenticate before anything touches the database, so anonymous
        // callers can neither write observations nor force unthrottled PAT lookups.
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments(McpPaths.McpEndpoint),
            branch => branch.UseRateLimiter(CreateMcpRateLimiterOptions()));
        app.UseMiddleware<McpRequestBodySizeMiddleware>();
        app.UseMiddleware<McpPatAuthMiddleware>();
        app.UseMiddleware<McpProtocolObservationMiddleware>();
        app.UseMiddleware<ScenarioWireMiddleware>();
        return app;
    }

    private static RateLimiterOptions CreateMcpRateLimiterOptions()
    {
        return new RateLimiterOptions
        {
            RejectionStatusCode = StatusCodes.Status429TooManyRequests,
            GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimiterPartitionKeys.Unknown,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimiterLimits.McpEndpointPermitLimit,
                        Window = TimeSpan.FromMinutes(RateLimiterLimits.McpEndpointWindowMinutes),
                        QueueLimit = 0,
                    })),
        };
    }
}
