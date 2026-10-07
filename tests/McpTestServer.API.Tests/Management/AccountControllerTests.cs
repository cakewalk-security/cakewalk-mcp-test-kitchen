using System.Net;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class AccountControllerTests
{
    private readonly McpTestServerApiFixture _fixture;

    public AccountControllerTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Login_redirects_to_login_page()
    {
        using var client = _fixture.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(LoginPaths.Page);
    }

    [Fact]
    public async Task Login_preserves_return_url_query()
    {
        using var client = _fixture.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Login?returnUrl=%2Fconsole");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/login?returnUrl=%2Fconsole");
    }

    [Fact]
    public async Task Login_github_returns_not_configured_until_oauth_is_enabled()
    {
        using var client = _fixture.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Login/GitHub?returnUrl=%2Fconsole");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/login-error?code=github_not_configured");
    }

    [Fact]
    public async Task Logout_redirects_to_login_page()
    {
        using var client = _fixture.CreateAuthenticatedClient();
        var response = await client.PostAsync("/Account/Logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(LoginPaths.Page);
    }

    [Fact]
    public async Task Root_redirects_unauthenticated_visitors_to_login_page()
    {
        using var client = _fixture.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(LoginPaths.Page);
    }
}

public sealed class AccountControllerNoGoogleAuthTests : IClassFixture<McpTestServerApiNoGoogleAuthFixture>
{
    private readonly McpTestServerApiNoGoogleAuthFixture _fixture;

    public AccountControllerNoGoogleAuthTests(McpTestServerApiNoGoogleAuthFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Login_google_returns_not_configured_when_credentials_missing()
    {
        using var client = _fixture.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Login/Google?returnUrl=%2Fconsole");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/login-error?code=google_not_configured");
    }
}
