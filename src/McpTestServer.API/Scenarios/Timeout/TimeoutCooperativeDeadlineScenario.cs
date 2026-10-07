using McpTestServer.API.Constants;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutCooperativeDeadlineScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.TimeoutCooperativeDeadline,
        "Cooperative deadline",
        "Delays run_configured_test_scenario for a configured duration, optionally honoring cancellation to exercise gateway timeout boundaries.",
        ScenarioAreas.Timeout,
        """{"delayMs":1000,"honorCancellation":true}""");

    public override async ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var parameters = session.GetParams<TimeoutCooperativeDeadlineParams>();
        if (parameters.DelayMs > 0)
        {
            var delayToken = parameters.HonorCancellation ? cancellationToken : CancellationToken.None;
            await Task.Delay(parameters.DelayMs, delayToken);
        }

        return ScenarioToolResults.Success(
            session,
            invocation,
            parameters.DelayMs,
            ReadOptionalMessage(arguments));
    }
}
