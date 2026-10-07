using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Baseline;

public sealed class BaselineParams
{
    [JsonPropertyName("slowReportDelayMs")]
    public int SlowReportDelayMs { get; init; }
}
