using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class JsonRpcErrorScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ErrorsJsonRpcError,
        "JSON-RPC error",
        "Returns a JSON-RPC error envelope (HTTP 200, error object in body) on a configured tools/call invocation.",
        ScenarioAreas.Errors,
        """{"code":-32602,"message":"Invalid params","onInvocation":1}""");

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);
        if (request?.Method != McpMethods.ToolsCall)
        {
            return WireDecision.Continue;
        }

        var invocation = context.Session.IncrementInvocationCount(ScenarioWireInvocationKeys.ToolsCall);
        var parameters = context.Session.GetParams<JsonRpcErrorParams>();
        if (!ScenarioWireHelpers.ShouldApplyOnInvocation(invocation, parameters.OnInvocation))
        {
            return WireDecision.Continue;
        }

        return WireDecision.RespondWithBody(
            StatusCodes.Status200OK,
            ScenarioWireJsonRpc.Error(request.Id, parameters.Code, parameters.Message));
    }
}
