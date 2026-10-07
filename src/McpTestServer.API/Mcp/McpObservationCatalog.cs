namespace McpTestServer.API.Mcp;

public static class McpObservationCatalog
{
    public const string CategoryAll = "all";

    public const string CategoryTools = "tools";

    public const string CategoryResources = "resources";

    public const string CategoryPrompts = "prompts";

    public static bool ShouldAttachScenarioId(string method) =>
        !method.StartsWith("resources/", StringComparison.Ordinal)
        && !method.StartsWith("prompts/", StringComparison.Ordinal);

    public static bool MatchesCategory(string method, string category) =>
        category switch
        {
            CategoryTools => method.StartsWith("tools/", StringComparison.Ordinal),
            CategoryResources => method.StartsWith("resources/", StringComparison.Ordinal),
            CategoryPrompts => method.StartsWith("prompts/", StringComparison.Ordinal),
            _ => true,
        };
}
