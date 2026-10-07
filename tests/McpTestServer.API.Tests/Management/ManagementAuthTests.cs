using System.Net;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ManagementAuthTests
{
    private readonly McpTestServerApiFixture _fixture;

    public ManagementAuthTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Me_with_auth_returns_email()
    {
        using var client = _fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/management/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("test-user@example.com");
        body.Should().Contain(McpProtocolConstants.LatestSupportedProtocolVersion);
        body.Should().Contain("\"mcpStateless\":true");
    }
}
