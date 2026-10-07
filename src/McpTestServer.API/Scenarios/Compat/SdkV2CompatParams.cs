using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Compat;

public sealed class SdkV2CompatParams
{
    public const string FallbackCloseReason = "completed";

    public const string FallbackPromptMessage =
        "Close ticket '{ticketId}'? Accept the default reason or provide your own.";

    [JsonPropertyName("defaultCloseReason")]
    public string DefaultCloseReason { get; init; } = FallbackCloseReason;

    [JsonPropertyName("promptMessage")]
    public string PromptMessage { get; init; } = FallbackPromptMessage;
}
