using FluentAssertions;
using McpTestServer.API.Services.Auth;
using Xunit;

namespace McpTestServer.API.Tests.Auth;

public sealed class OAuthAccountSelectionTests
{
    [Fact]
    public void WrapGitHubAuthorizeUrl_routes_through_github_login()
    {
        const string authorizeUrl =
            "https://github.com/login/oauth/authorize?client_id=test&redirect_uri=https%3A%2F%2Fexample.com%2Fcallback";

        var wrapped = OAuthAccountSelection.WrapGitHubAuthorizeUrl(authorizeUrl);

        wrapped.Should().StartWith("https://github.com/login?return_to=");
        wrapped.Should().Contain(Uri.EscapeDataString(authorizeUrl));
    }
}
