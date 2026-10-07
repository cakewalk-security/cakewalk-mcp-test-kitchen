using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class HttpStatusPerMethodScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ErrorsHttpStatusPerMethod,
        "HTTP status per method",
        "Returns different HTTP status codes based on the JSON-RPC method in the request body.",
        ScenarioAreas.Errors,
        """{"rules":{"tools/call":401,"initialize":200}}""");

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);
        if (request?.Method is null)
        {
            return WireDecision.Continue;
        }

        var parameters = context.Session.GetParams<HttpStatusPerMethodParams>();
        var rules = parameters.Rules;
        if (rules is null || !rules.TryGetValue(request.Method, out var statusCode))
        {
            return WireDecision.Continue;
        }

        if (statusCode is >= 200 and < 300)
        {
            return WireDecision.Continue;
        }

        return WireDecision.RespondWithStatus(statusCode);
    }
}
