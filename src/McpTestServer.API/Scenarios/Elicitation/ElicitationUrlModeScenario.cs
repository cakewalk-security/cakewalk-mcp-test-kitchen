using McpTestServer.API.Constants;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Elicitation;

public sealed class ElicitationUrlModeScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ElicitationUrlMode,
        "Elicitation URL mode",
        "Calls MCP elicitation with mode=url so the gateway can verify downstream URL capability forwarding.",
        ScenarioAreas.Elicitation,
        """
        {
          "url": "https://example.com/approve",
          "promptMessage": "Open the URL to approve this MCP test scenario."
        }
        """);

    public override ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var parameters = session.GetParams<ElicitationUrlModeParams>();
        var message = ReadOptionalMessage(arguments);

        if (ElicitationMrtrHelper.TryGetElicitResult(request, out var elicit))
        {
            return ValueTask.FromResult(ElicitationApprovalScenario.BuildOutcome(session, invocation, elicit, message));
        }

        if (!request.Server.IsMrtrSupported)
        {
            return ValueTask.FromResult(ScenarioToolResults.ElicitationOutcome(
                session,
                invocation,
                outcome: "unsupported",
                reason: null,
                message,
                isError: true));
        }

        ElicitationMrtrHelper.ThrowElicitation(new ElicitRequestParams
        {
            Mode = McpElicitationModes.Url,
            Message = parameters.PromptMessage,
            Url = parameters.Url,
        });
        return default;
    }
}
