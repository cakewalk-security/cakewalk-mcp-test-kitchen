using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class ConnectionCloseScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ErrorsConnectionClose,
        "Connection close",
        "Abruptly closes the TCP connection during initialize or tools/call on a configured invocation.",
        ScenarioAreas.Errors,
        """{"closeOn":"tool_call","onInvocation":1}""");

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);
        if (request?.Method is null)
        {
            return WireDecision.Continue;
        }

        var parameters = context.Session.GetParams<ConnectionCloseParams>();
        var matchesTarget = parameters.CloseOn switch
        {
            ConnectionCloseTargets.Initialize => request.Method == McpMethods.Initialize,
            ConnectionCloseTargets.ToolCall => request.Method == McpMethods.ToolsCall,
            _ => false,
        };

        if (!matchesTarget)
        {
            return WireDecision.Continue;
        }

        var invocationKey = parameters.CloseOn == ConnectionCloseTargets.Initialize
            ? ScenarioWireInvocationKeys.HttpRequest
            : ScenarioWireInvocationKeys.ToolsCall;
        var invocation = context.Session.IncrementInvocationCount(invocationKey);
        if (!ScenarioWireHelpers.ShouldApplyOnInvocation(invocation, parameters.OnInvocation))
        {
            return WireDecision.Continue;
        }

        return WireDecision.CloseConnection();
    }
}
