using McpTestServer.API.Constants;
using McpTestServer.Infrastructure;
using McpTestServer.Infrastructure.Constants;
using Microsoft.EntityFrameworkCore;

namespace McpTestServer.API.Scenarios;

internal static class ScenarioSessionResolver
{
    public static async ValueTask<ScenarioSession> ResolveAsync(
        HttpContext context,
        IScenarioSessionRegistry sessionRegistry,
        IScenarioSessionFactory sessionFactory,
        McpTestServerContext db,
        CancellationToken cancellationToken)
    {
        if (context.Items.TryGetValue(ScenarioSessionItemKeys.ScenarioSession, out var itemSession)
            && itemSession is ScenarioSession configuredSession)
        {
            return configuredSession;
        }

        var callerEmail = context.Items[McpCallerConstants.CallerEmailItemKey] as string;
        var mcpSessionId = context.Request.Headers[AuthHeaderNames.McpSessionId].FirstOrDefault();
        // Mcp-Session-Id is client-chosen (the transport is stateless), so only honor it
        // when it points at a session owned by the authenticated caller.
        if (sessionRegistry.TryGet(mcpSessionId, out var registeredSession)
            && registeredSession is not null
            && registeredSession.BelongsTo(callerEmail))
        {
            context.Items[ScenarioSessionItemKeys.ScenarioSession] = registeredSession;
            return registeredSession;
        }

        var request = await ScenarioWireRequestReader.TryReadPostAsync(context.Request, cancellationToken);
        var isNewSessionInitialize = request?.Method == McpMethods.Initialize
            && string.IsNullOrWhiteSpace(mcpSessionId);

        if (!isNewSessionInitialize
            && sessionFactory.TryGetForCaller(callerEmail, out var activeSession)
            && activeSession is not null)
        {
            context.Items[ScenarioSessionItemKeys.ScenarioSession] = activeSession;
            return activeSession;
        }

        var (scenarioId, paramsJson) = await ResolveSelectionAsync(db, callerEmail, cancellationToken);
        var session = sessionFactory.GetOrCreate(callerEmail, scenarioId, paramsJson);
        context.Items[ScenarioSessionItemKeys.ScenarioSession] = session;
        return session;
    }

    public static async ValueTask<(string ScenarioId, string ParamsJson)> ResolveSelectionAsync(
        McpTestServerContext db,
        string? callerEmail,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(callerEmail))
        {
            return (ScenarioIds.Baseline, "{}");
        }

        var selection = await db.UserScenarioSelections
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email == callerEmail, cancellationToken);

        if (selection is null)
        {
            return (ScenarioIds.Baseline, "{}");
        }

        return (selection.ScenarioId, selection.ParamsJson);
    }
}
