using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Controllers.Account;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class AuthStatusTests
{
    private readonly McpTestServerApiFixture _fixture;

    public AuthStatusTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Auth_status_without_auth_returns_authenticated_false()
    {
        using var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/auth/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        body.Should().NotBeNull();
        body!.Authenticated.Should().BeFalse();
    }

    [Fact]
    public async Task Auth_status_with_auth_returns_authenticated_true()
    {
        using var client = _fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/auth/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        body.Should().NotBeNull();
        body!.Authenticated.Should().BeTrue();
    }
}
