using McpTestServer.API.Constants;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutUncooperativeHangScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.TimeoutUncooperativeHang,
        "Uncooperative hang",
        "Hangs and ignores cancellation on a configured run_configured_test_scenario invocation, then succeeds on the next call.",
        ScenarioAreas.Timeout,
        """{"hangOnInvocation":1,"hangDurationMs":60000}""");

    public override async ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var parameters = session.GetParams<TimeoutUncooperativeHangParams>();
        if (invocation == parameters.HangOnInvocation && parameters.HangDurationMs > 0)
        {
            await Task.Delay(parameters.HangDurationMs, CancellationToken.None);
        }

        return ScenarioToolResults.Success(session, invocation, message: ReadOptionalMessage(arguments));
    }
}
