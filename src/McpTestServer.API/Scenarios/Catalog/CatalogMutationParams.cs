using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class CatalogMutationEntry
{
    [JsonPropertyName("action")]
    public string Action { get; init; } = CatalogMutationActions.Add;

    [JsonPropertyName("toolName")]
    public string ToolName { get; init; } = "extra_tool";

    [JsonPropertyName("afterMs")]
    public int AfterMs { get; init; }

    [JsonPropertyName("newName")]
    public string? NewName { get; init; }

    [JsonPropertyName("listChangedNotifications")]
    public int ListChangedNotifications { get; init; } = 1;
}

public sealed class CatalogMutationParams
{
    [JsonPropertyName("mutations")]
    public CatalogMutationEntry[] Mutations { get; init; } = [];
}
