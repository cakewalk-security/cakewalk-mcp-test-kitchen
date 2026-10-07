namespace McpTestServer.API.Scenarios.Errors;

public sealed class HttpStatusSequenceScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ErrorsHttpStatusSequence,
        "HTTP transport error",
        "Returns a configured sequence of HTTP status codes across successive MCP POST requests (transport layer).",
        ScenarioAreas.Errors,
        """{"statusCodes":[503,200]}""");

    public override ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            return ValueTask.FromResult(WireDecision.Continue);
        }

        var invocation = context.Session.IncrementInvocationCount(ScenarioWireInvocationKeys.HttpRequest);
        var parameters = context.Session.GetParams<HttpStatusSequenceParams>();
        var statusCodes = parameters.StatusCodes ?? [];
        if (statusCodes.Length == 0)
        {
            return ValueTask.FromResult(WireDecision.Continue);
        }

        var index = Math.Min(invocation - 1, statusCodes.Length - 1);
        var statusCode = statusCodes[index];
        if (statusCode is >= 200 and < 300)
        {
            return ValueTask.FromResult(WireDecision.Continue);
        }

        return ValueTask.FromResult(WireDecision.RespondWithStatus(statusCode));
    }
}
