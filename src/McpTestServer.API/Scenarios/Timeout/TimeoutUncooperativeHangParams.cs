using System.Text.Json.Serialization;
using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Timeout;

public sealed class TimeoutUncooperativeHangParams : IScenarioParamsValidation
{
    public const string HangDurationMsMustBeNonNegative = "hangDurationMs must be greater than or equal to 0.";

    [JsonPropertyName("hangOnInvocation")]
    public int HangOnInvocation { get; init; } = 1;

    [JsonPropertyName("hangDurationMs")]
    public int HangDurationMs { get; init; } = 60_000;

    public string? Validate() =>
        HangDurationMs < 0 ? HangDurationMsMustBeNonNegative : null;
}
