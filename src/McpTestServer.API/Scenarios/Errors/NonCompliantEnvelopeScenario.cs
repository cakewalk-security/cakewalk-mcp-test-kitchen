using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class NonCompliantEnvelopeScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ErrorsNonCompliantEnvelope,
        "Non-compliant envelope",
        "Returns a non-2xx HTTP status with a valid JSON-RPC result body to exercise upstream envelope normalization.",
        ScenarioAreas.Errors,
        """
        {
          "httpStatus": 400,
          "resultPayload": "{\"content\":[{\"type\":\"text\",\"text\":\"ok\"}],\"isError\":false}",
          "onInvocation": 1
        }
        """);

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            return WireDecision.Continue;
        }

        var invocation = ScenarioWireHelpers.IncrementHttpRequestInvocation(context.Session);
        var parameters = context.Session.GetParams<NonCompliantEnvelopeParams>();
        if (!ScenarioWireHelpers.ShouldApplyOnInvocation(invocation, parameters.OnInvocation))
        {
            return WireDecision.Continue;
        }

        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);

        var body = $$"""
                     {"jsonrpc":"2.0","id":{{ScenarioWireJsonRpc.FormatId(request?.Id)}},"result":{{parameters.ResultPayload}}}
                     """;

        return WireDecision.RespondWithBody(parameters.HttpStatus, body);
    }
}
