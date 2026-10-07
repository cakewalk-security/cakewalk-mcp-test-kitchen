namespace McpTestServer.Infrastructure.Options;

public sealed class GitHubCredentialsOptions
{
    public string INTERNAL_AUTHENTICATION_GITHUB_CLIENT_SECRET { get; set; } = string.Empty;

    public string INTERNAL_AUTHENTICATION_GITHUB_CLIENT_ID { get; set; } = string.Empty;
}
