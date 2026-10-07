using System.Net;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Options;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerLegacyHostRedirectApiCollection))]
public sealed class LegacyHostRedirectIntegrationTests
{
    private readonly McpTestServerLegacyHostRedirectApiFixture _fixture;

    public LegacyHostRedirectIntegrationTests(McpTestServerLegacyHostRedirectApiFixture fixture) =>
        _fixture = fixture;

    [Fact]
    public async Task Legacy_host_requests_are_redirected_to_canonical_origin()
    {
        using var client = _fixture.CreateClient(new() { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/login?returnUrl=%2Fconsole");
        request.Headers.Host = "mcp-test-server-cakewalk.fly.dev";

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PermanentRedirect);
        response.Headers.Location!.ToString()
            .Should()
            .Be("https://mcp-test-kitchen.cakewalk.security/login?returnUrl=%2Fconsole");
    }

    [Fact]
    public async Task Legacy_host_health_checks_are_not_redirected()
    {
        using var client = _fixture.CreateClient(new() { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, RequestLoggingConstants.HealthPath);
        request.Headers.Host = "mcp-test-server-cakewalk.fly.dev";

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public sealed class McpTestServerLegacyHostRedirectApiFixture : McpTestServerApiFixture
{
    protected override string EnvironmentName => "Production";

    protected override Dictionary<string, string?> BuildConfiguration()
    {
        var config = base.BuildConfiguration();
        config[LegacyHostRedirectOptions.CanonicalOriginEnvKey] = "https://mcp-test-kitchen.cakewalk.security";
        config[LegacyHostRedirectOptions.LegacyHostsEnvKey] = "mcp-test-server-cakewalk.fly.dev";
        config[OAuthPublicOriginOptions.EnvKey] = "https://mcp-test-kitchen.cakewalk.security";
        return config;
    }
}

[CollectionDefinition(nameof(McpTestServerLegacyHostRedirectApiCollection))]
public sealed class McpTestServerLegacyHostRedirectApiCollection
    : ICollectionFixture<McpTestServerLegacyHostRedirectApiFixture>
{
}
