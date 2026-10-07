using System.ComponentModel.DataAnnotations;

namespace McpTestServer.API.Options;

public sealed class UnauthenticatedRedirectOptions
{
    public const string EnvKey = "MCP_TEST_SERVER_UNAUTHENTICATED_REDIRECT_URL";

    public const string DefaultUrl = "https://www.cakewalk.security/mcp-test-kitchen";

    [Required]
    public string MCP_TEST_SERVER_UNAUTHENTICATED_REDIRECT_URL { get; set; } = DefaultUrl;
}
