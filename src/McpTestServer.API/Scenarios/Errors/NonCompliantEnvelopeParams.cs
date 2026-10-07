using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class NonCompliantEnvelopeParams
{
    [JsonPropertyName("httpStatus")]
    public int HttpStatus { get; init; } = StatusCodes.Status400BadRequest;

    [JsonPropertyName("resultPayload")]
    public string ResultPayload { get; init; } =
        """{"content":[{"type":"text","text":"ok"}],"isError":false}""";

    [JsonPropertyName("onInvocation")]
    public int OnInvocation { get; init; } = 1;
}
