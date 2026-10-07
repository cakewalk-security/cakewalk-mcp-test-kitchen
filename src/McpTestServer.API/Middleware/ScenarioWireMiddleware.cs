using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Baseline;
using McpTestServer.Infrastructure;
using McpTestServer.Infrastructure.Constants;

namespace McpTestServer.API.Middleware;

public sealed class ScenarioWireMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IScenarioSessionRegistry sessionRegistry,
        IScenarioSessionFactory sessionFactory,
        IScenarioCatalog scenarioCatalog,
        McpTestServerContext db)
    {
        if (!context.Request.Path.StartsWithSegments(McpPaths.McpEndpoint))
        {
            await next(context);
            return;
        }

        var session = await ScenarioSessionResolver.ResolveAsync(
            context,
            sessionRegistry,
            sessionFactory,
            db,
            context.RequestAborted);

        RegisterIncomingSessionId(context, sessionRegistry, session);
        session.AttachRequest(context);
        try
        {
            if (!scenarioCatalog.TryGet(session.ScenarioId, out var scenario) || scenario is null)
            {
                await next(context);
                RegisterIssuedSessionId(context, sessionRegistry, session);
                return;
            }

            var baselineDecision = await BaselineCatalogWireInterceptor.TryInterceptAsync(
                context,
                context.RequestAborted);
            if (baselineDecision.Kind != WireDecisionKind.Continue)
            {
                await ApplyWireDecisionAsync(context, baselineDecision);
                RegisterIssuedSessionId(context, sessionRegistry, session);
                return;
            }

            var decision = await scenario.OnRequestAsync(new ScenarioWireContext
            {
                Session = session,
                HttpContext = context,
                CancellationToken = context.RequestAborted,
            });

            if (decision.DelayMs is > 0)
            {
                var delayToken = decision.HonorCancellationDuringDelay
                    ? context.RequestAborted
                    : CancellationToken.None;
                await Task.Delay(decision.DelayMs.Value, delayToken);
            }

            if (decision.Kind == WireDecisionKind.Continue)
            {
                await next(context);
                RegisterIssuedSessionId(context, sessionRegistry, session);
                return;
            }

            await ApplyWireDecisionAsync(context, decision);
            if (decision.Kind != WireDecisionKind.CloseConnection)
            {
                RegisterIssuedSessionId(context, sessionRegistry, session);
            }
        }
        finally
        {
            session.DetachRequest(context);
        }
    }

    private static async Task ApplyWireDecisionAsync(HttpContext context, WireDecision decision)
    {
        switch (decision.Kind)
        {
            case WireDecisionKind.RespondWithStatus:
                context.Response.StatusCode = decision.StatusCode ?? StatusCodes.Status500InternalServerError;
                return;
            case WireDecisionKind.RespondWithBody:
                context.Response.StatusCode = decision.StatusCode ?? StatusCodes.Status500InternalServerError;
                context.Response.ContentType = HttpMediaTypes.ApplicationJson;
                await context.Response.WriteAsync(decision.Body ?? string.Empty, context.RequestAborted);
                return;
            case WireDecisionKind.CloseConnection:
                context.Abort();
                return;
        }
    }

    private static void RegisterIncomingSessionId(
        HttpContext context,
        IScenarioSessionRegistry sessionRegistry,
        ScenarioSession session)
    {
        var mcpSessionId = context.Request.Headers[AuthHeaderNames.McpSessionId].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(mcpSessionId))
        {
            return;
        }

        // Never re-point an ID that already belongs to another caller's session.
        if (sessionRegistry.TryGet(mcpSessionId, out var existing) && existing is not null)
        {
            return;
        }

        sessionRegistry.Register(mcpSessionId, session);
    }

    private static void RegisterIssuedSessionId(
        HttpContext context,
        IScenarioSessionRegistry sessionRegistry,
        ScenarioSession session)
    {
        var mcpSessionId = context.Response.Headers[AuthHeaderNames.McpSessionId].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(mcpSessionId))
        {
            sessionRegistry.Register(mcpSessionId, session);
        }
    }
}
