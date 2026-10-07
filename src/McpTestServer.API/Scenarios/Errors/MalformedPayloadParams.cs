using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class MalformedPayloadParams
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = MalformedPayloadKinds.InvalidJson;

    [JsonPropertyName("target")]
    public string Target { get; init; } = MalformedPayloadTargets.ToolCall;

    [JsonPropertyName("onInvocation")]
    public int OnInvocation { get; init; } = 1;
}
