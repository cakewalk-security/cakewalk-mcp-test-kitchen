using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutCooperativeDeadlineParams
{
    [JsonPropertyName("delayMs")]
    public int DelayMs { get; init; } = 1000;

    [JsonPropertyName("honorCancellation")]
    public bool HonorCancellation { get; init; } = true;
}
