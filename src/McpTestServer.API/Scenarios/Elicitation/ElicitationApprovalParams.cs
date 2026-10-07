using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Elicitation;

public sealed class ElicitationApprovalParams
{
    public const string DefaultPromptMessage =
        "Approve running this MCP test scenario? Provide a short reason for the audit log.";

    public const string DefaultReasonFieldTitle = "Reason (recorded for audit)";

    public const int DefaultReasonMaxLength = 200;

    // Matches the gateway's UPSTREAM_ELICITATION_TIMEOUT. A human has to read and answer the
    // prompt, so a short budget makes the tool abandon the elicitation before it can be shown.
    // Lower it deliberately to exercise the timeout path.
    public const int DefaultElicitationTimeoutMs = 300_000;

    [JsonPropertyName("promptMessage")]
    public string PromptMessage { get; init; } = DefaultPromptMessage;

    [JsonPropertyName("reasonFieldTitle")]
    public string ReasonFieldTitle { get; init; } = DefaultReasonFieldTitle;

    [JsonPropertyName("reasonMaxLength")]
    public int ReasonMaxLength { get; init; } = DefaultReasonMaxLength;

    [JsonPropertyName("requireReason")]
    public bool RequireReason { get; init; } = true;

    [JsonPropertyName("elicitationTimeoutMs")]
    public int ElicitationTimeoutMs { get; init; } = DefaultElicitationTimeoutMs;
}
