using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutInitHangScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.TimeoutInitHang,
        "Initialize hang",
        "Delays the initialize MCP request (2025-11-25 back-compat) to exercise upstream connection initialization timeouts.",
        ScenarioAreas.Timeout,
        """{"initDelayMs":2000,"honorCancellation":true}""");

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);
        if (request?.Method != McpMethods.Initialize)
        {
            return WireDecision.Continue;
        }

        var parameters = context.Session.GetParams<TimeoutInitHangParams>();
        if (parameters.InitDelayMs <= 0)
        {
            return WireDecision.Continue;
        }

        return WireDecision.Delay(parameters.InitDelayMs, parameters.HonorCancellation);
    }
}
