namespace McpTestServer.API.Options;

public sealed class McpPatOptions
{
    public const string SectionName = "McpTestServer";

    public string MCP_PAT { get; set; } = string.Empty;
}
