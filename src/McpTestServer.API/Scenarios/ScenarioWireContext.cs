namespace McpTestServer.API.Scenarios;

public sealed class ScenarioWireContext
{
    public required ScenarioSession Session { get; init; }

    public required HttpContext HttpContext { get; init; }

    public required CancellationToken CancellationToken { get; init; }
}
