using McpTestServer.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Services.Auth;

public interface IGitHubAuthAvailability
{
    bool IsConfigured { get; }
}

public sealed class GitHubAuthAvailability(IOptions<GitHubCredentialsOptions> options) : IGitHubAuthAvailability
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.Value.INTERNAL_AUTHENTICATION_GITHUB_CLIENT_ID);
}
