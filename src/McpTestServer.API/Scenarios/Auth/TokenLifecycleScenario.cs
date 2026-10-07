using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Auth;

public sealed class TokenLifecycleScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.AuthTokenLifecycle,
        "Token lifecycle",
        "Rejects MCP POST requests with HTTP 401 until a configured invocation, then accepts. Use rejectForever to simulate unrecoverable auth.",
        ScenarioAreas.Auth,
        """{"rejectUntilInvocation":1,"rejectForever":false}""");

    public override ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            return ValueTask.FromResult(WireDecision.Continue);
        }

        var invocation = ScenarioWireHelpers.IncrementHttpRequestInvocation(context.Session);
        var parameters = context.Session.GetParams<TokenLifecycleParams>();
        if (ShouldRejectInvocation(invocation, parameters))
        {
            return ValueTask.FromResult(WireDecision.RespondWithStatus(StatusCodes.Status401Unauthorized));
        }

        return ValueTask.FromResult(WireDecision.Continue);
    }

    internal static bool ShouldRejectInvocation(int invocation, TokenLifecycleParams parameters) =>
        parameters.RejectForever || invocation <= parameters.RejectUntilInvocation;
}
