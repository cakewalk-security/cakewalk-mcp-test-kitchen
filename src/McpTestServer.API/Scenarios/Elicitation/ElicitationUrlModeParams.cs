using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Elicitation;

public sealed class ElicitationUrlModeParams
{
    public const string DefaultUrl = "https://example.com/approve";

    public const int DefaultElicitationTimeoutMs = 300_000;

    [JsonPropertyName("url")]
    public string Url { get; init; } = DefaultUrl;

    [JsonPropertyName("promptMessage")]
    public string PromptMessage { get; init; } = "Open the URL to approve this MCP test scenario.";

    [JsonPropertyName("elicitationTimeoutMs")]
    public int ElicitationTimeoutMs { get; init; } = DefaultElicitationTimeoutMs;
}
