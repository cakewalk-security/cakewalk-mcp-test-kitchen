namespace McpTestServer.Infrastructure.Entities;

public sealed class UserScenarioSelection
{
    public string Email { get; set; } = string.Empty;

    public string ScenarioId { get; set; } = string.Empty;

    public string ParamsJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; }
}
