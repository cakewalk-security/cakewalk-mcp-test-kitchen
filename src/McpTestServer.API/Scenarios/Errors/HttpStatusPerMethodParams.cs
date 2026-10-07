using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class HttpStatusPerMethodParams
{
    [JsonPropertyName("rules")]
    public Dictionary<string, int> Rules { get; init; } = new(StringComparer.Ordinal)
    {
        ["tools/call"] = StatusCodes.Status401Unauthorized,
        ["initialize"] = StatusCodes.Status200OK,
    };
}
