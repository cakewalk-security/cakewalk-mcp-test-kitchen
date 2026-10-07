using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class ConnectionCloseParams
{
    [JsonPropertyName("closeOn")]
    public string CloseOn { get; init; } = ConnectionCloseTargets.ToolCall;

    [JsonPropertyName("onInvocation")]
    public int OnInvocation { get; init; } = 1;
}
