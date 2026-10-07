using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Auth;

public sealed class TokenLifecycleParams
{
    [JsonPropertyName("rejectUntilInvocation")]
    public int RejectUntilInvocation { get; init; } = 1;

    [JsonPropertyName("rejectForever")]
    public bool RejectForever { get; init; }
}
