using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutLateResponseParams
{
    [JsonPropertyName("delayMs")]
    public int DelayMs { get; init; } = 5000;
}
