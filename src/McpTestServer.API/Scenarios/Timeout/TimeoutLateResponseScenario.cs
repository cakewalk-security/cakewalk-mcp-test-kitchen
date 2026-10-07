using McpTestServer.API.Constants;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutLateResponseScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.TimeoutLateResponse,
        "Late response",
        "Completes run_configured_test_scenario after a long delay while ignoring cancellation, simulating a late upstream result after the gateway times out.",
        ScenarioAreas.Timeout,
        """{"delayMs":5000}""");

    public override async ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var parameters = session.GetParams<TimeoutLateResponseParams>();
        if (parameters.DelayMs > 0)
        {
            try
            {
                await Task.Delay(parameters.DelayMs, CancellationToken.None);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation for this scenario.
            }
        }

        return ScenarioToolResults.Success(
            session,
            invocation,
            parameters.DelayMs,
            ReadOptionalMessage(arguments));
    }
}
