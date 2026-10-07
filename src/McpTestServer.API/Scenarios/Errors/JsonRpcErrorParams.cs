using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class JsonRpcErrorParams
{
    [JsonPropertyName("code")]
    public int Code { get; init; } = -32602;

    [JsonPropertyName("message")]
    public string Message { get; init; } = "Invalid params";

    [JsonPropertyName("onInvocation")]
    public int OnInvocation { get; init; } = 1;
}
