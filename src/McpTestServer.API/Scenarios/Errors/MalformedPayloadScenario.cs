using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class MalformedPayloadScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ErrorsMalformedPayload,
        "Malformed payload",
        "Returns syntactically invalid JSON or structurally wrong JSON-RPC responses on a configured tools/call invocation (by default). Use target=initialize or target=http_request to fault earlier in the session.",
        ScenarioAreas.Errors,
        """{"kind":"invalid_json","target":"tool_call","onInvocation":1}""");

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);
        if (request?.Method is null)
        {
            return WireDecision.Continue;
        }

        var parameters = context.Session.GetParams<MalformedPayloadParams>();
        if (!MatchesTarget(parameters.Target, request.Method))
        {
            return WireDecision.Continue;
        }

        var invocationKey = ResolveInvocationKey(parameters.Target, request.Method);
        var invocation = context.Session.IncrementInvocationCount(invocationKey);
        if (!ScenarioWireHelpers.ShouldApplyOnInvocation(invocation, parameters.OnInvocation))
        {
            return WireDecision.Continue;
        }

        var body = parameters.Kind switch
        {
            MalformedPayloadKinds.InvalidJson => "{ not valid json",
            MalformedPayloadKinds.WrongId => ScenarioWireJsonRpc.Error(99999, -32602, "Wrong id"),
            MalformedPayloadKinds.MissingJsonRpc => """{"id":1,"result":{}}""",
            MalformedPayloadKinds.DuplicateResponse =>
                $"{ScenarioWireJsonRpc.Result(request.Id, new { ok = true })}\n{ScenarioWireJsonRpc.Result(request.Id, new { ok = true })}",
            _ => "{ not valid json",
        };

        return WireDecision.RespondWithBody(StatusCodes.Status200OK, body);
    }

    private static bool MatchesTarget(string target, string method) =>
        target switch
        {
            MalformedPayloadTargets.ToolCall => method == McpMethods.ToolsCall,
            MalformedPayloadTargets.Initialize => method == McpMethods.Initialize,
            MalformedPayloadTargets.HttpRequest => true,
            _ => method == McpMethods.ToolsCall,
        };

    private static string ResolveInvocationKey(string target, string method) =>
        target switch
        {
            MalformedPayloadTargets.ToolCall => ScenarioWireInvocationKeys.ToolsCall,
            MalformedPayloadTargets.Initialize => ScenarioWireInvocationKeys.HttpRequest,
            MalformedPayloadTargets.HttpRequest => ScenarioWireInvocationKeys.HttpRequest,
            _ => method == McpMethods.ToolsCall
                ? ScenarioWireInvocationKeys.ToolsCall
                : ScenarioWireInvocationKeys.HttpRequest,
        };
}
