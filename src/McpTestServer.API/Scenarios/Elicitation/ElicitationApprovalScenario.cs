using System.Text.Json;
using McpTestServer.API.Constants;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Elicitation;

public sealed class ElicitationApprovalScenario : ScenarioBase
{
    public const string ReasonFieldName = "reason";

    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ElicitationApproval,
        "Elicitation approval",
        "Calls MCP elicitation during run_configured_test_scenario so the client can prompt the user for approval before continuing.",
        ScenarioAreas.Elicitation,
        """
        {
          "promptMessage": "Approve running this MCP test scenario? Provide a short reason for the audit log.",
          "reasonFieldTitle": "Reason (recorded for audit)",
          "reasonMaxLength": 200,
          "requireReason": true
        }
        """);

    public override ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var parameters = session.GetParams<ElicitationApprovalParams>();
        var message = ReadOptionalMessage(arguments);

        if (ElicitationMrtrHelper.TryGetElicitResult(request, out var elicit))
        {
            return ValueTask.FromResult(BuildOutcome(session, invocation, elicit, message));
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
            RequestedSchema = BuildSchema(parameters),
        });
        return default;
    }

    private static ElicitRequestParams.RequestSchema BuildSchema(ElicitationApprovalParams parameters) =>
        new()
        {
            Properties =
            {
                [ReasonFieldName] = new ElicitRequestParams.StringSchema
                {
                    Title = parameters.ReasonFieldTitle,
                    MaxLength = parameters.ReasonMaxLength,
                },
            },
            Required = parameters.RequireReason ? [ReasonFieldName] : null,
        };

    internal static CallToolResult BuildOutcome(
        ScenarioSession session,
        int invocation,
        ElicitResult? elicit,
        string? message)
    {
        if (elicit is null)
        {
            return ScenarioToolResults.ElicitationOutcome(
                session,
                invocation,
                outcome: "unsupported",
                reason: null,
                message,
                isError: true);
        }

        string? reason = null;
        if (elicit.Content?.TryGetValue(ReasonFieldName, out var reasonElement) == true
            && reasonElement.ValueKind == JsonValueKind.String)
        {
            reason = reasonElement.GetString();
        }

        if (elicit.IsAccepted)
        {
            return ScenarioToolResults.ElicitationOutcome(
                session,
                invocation,
                outcome: "accepted",
                reason,
                message);
        }

        return ScenarioToolResults.ElicitationOutcome(
            session,
            invocation,
            outcome: elicit.Action,
            reason,
            message,
            isError: true);
    }
}
