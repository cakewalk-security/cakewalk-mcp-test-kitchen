using McpTestServer.API.Constants;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Elicitation;

public sealed class ElicitationDeclineScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ElicitationDecline,
        "Elicitation decline",
        "Calls MCP elicitation during run_configured_test_scenario. Decline or cancel from the downstream client to exercise upstream elicitation failure paths.",
        ScenarioAreas.Elicitation,
        """
        {
          "expectedOutcome": "decline",
          "promptMessage": "Decline or cancel this elicitation to exercise upstream elicitation failure paths."
        }
        """);

    public override ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var parameters = session.GetParams<ElicitationDeclineParams>();
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
            Mode = McpElicitationModes.Form,
            Message = parameters.PromptMessage,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["confirm"] = new ElicitRequestParams.StringSchema
                    {
                        Title = "Type confirm to proceed",
                        MaxLength = 32,
                    },
                },
            },
        });
        return default;
    }
}
