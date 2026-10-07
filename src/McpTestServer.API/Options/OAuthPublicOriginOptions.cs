using System.ComponentModel.DataAnnotations;

namespace McpTestServer.API.Options;

public sealed class OAuthPublicOriginOptions
{
    public const string EnvKey = "MCP_TEST_SERVER_OAUTH_PUBLIC_ORIGIN";

    [Required]
    public string MCP_TEST_SERVER_OAUTH_PUBLIC_ORIGIN { get; set; } = string.Empty;
}
