using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Elicitation;

public static class ElicitationExpectedOutcomes
{
    public const string Decline = "decline";

    public const string Cancel = "cancel";

    public const string Timeout = "timeout";
}

public sealed class ElicitationDeclineParams
{
    public const int DefaultElicitationTimeoutMs = 5_000;

    public const string DefaultPromptMessage =
        "Decline or cancel this elicitation to exercise upstream elicitation failure paths.";

    [JsonPropertyName("expectedOutcome")]
    public string ExpectedOutcome { get; init; } = ElicitationExpectedOutcomes.Decline;

    [JsonPropertyName("promptMessage")]
    public string PromptMessage { get; init; } = DefaultPromptMessage;

    [JsonPropertyName("elicitationTimeoutMs")]
    public int ElicitationTimeoutMs { get; init; } = DefaultElicitationTimeoutMs;
}
