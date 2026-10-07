using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutInitHangParams
{
    [JsonPropertyName("initDelayMs")]
    public int InitDelayMs { get; init; } = 2000;

    [JsonPropertyName("honorCancellation")]
    public bool HonorCancellation { get; init; } = true;
}
