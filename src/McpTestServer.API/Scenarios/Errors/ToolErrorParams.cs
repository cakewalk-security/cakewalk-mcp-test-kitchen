using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class ToolErrorParams
{
    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; init; } = "Tool-side failure";

    [JsonPropertyName("onInvocation")]
    public int OnInvocation { get; init; } = 1;
}
