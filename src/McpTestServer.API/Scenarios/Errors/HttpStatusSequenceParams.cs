using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class HttpStatusSequenceParams
{
    [JsonPropertyName("statusCodes")]
    public int[] StatusCodes { get; init; } = [503, 200];
}
