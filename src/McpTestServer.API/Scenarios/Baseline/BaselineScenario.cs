using McpTestServer.API.Constants;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Baseline;

public sealed class BaselineScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.Baseline,
        "Baseline (success)",
        "Healthy MCP server. run_configured_test_scenario returns a successful tool result; optional delay before returning.",
        ScenarioAreas.Baseline,
        """{"slowReportDelayMs":0}""");

    public override async ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var delayMs = session.GetParams<BaselineParams>().SlowReportDelayMs;
        if (delayMs > 0)
        {
            await Task.Delay(delayMs, cancellationToken);
        }

        return ScenarioToolResults.Success(
            session,
            invocation,
            delayMs,
            ReadOptionalMessage(arguments));
    }
}
