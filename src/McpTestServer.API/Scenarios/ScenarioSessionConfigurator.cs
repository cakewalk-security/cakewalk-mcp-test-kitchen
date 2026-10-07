using McpTestServer.API.Constants;
using McpTestServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios;

public static class ScenarioSessionConfigurator
{
    public static async Task ConfigureSessionOptionsAsync(
        HttpContext httpContext,
        McpServerOptions options,
        CancellationToken cancellationToken)
    {
        var callerEmail = httpContext.Items[McpCallerConstants.CallerEmailItemKey] as string;
        var catalog = httpContext.RequestServices.GetRequiredService<IScenarioCatalog>();
        var sessionFactory = httpContext.RequestServices.GetRequiredService<IScenarioSessionFactory>();
        var db = httpContext.RequestServices.GetRequiredService<McpTestServerContext>();

        var (scenarioId, paramsJson) = await ScenarioSessionResolver.ResolveSelectionAsync(
            db,
            callerEmail,
            cancellationToken);
        var session = sessionFactory.GetOrCreate(callerEmail, scenarioId, paramsJson);
        httpContext.Items[ScenarioSessionItemKeys.ScenarioSession] = session;

        var activeScenario = catalog.TryGet(session.ScenarioId, out var resolved)
            ? resolved!
            : catalog.GetRequired(ScenarioIds.Baseline);
        activeScenario.ConfigureSession(options, session);
    }
}
