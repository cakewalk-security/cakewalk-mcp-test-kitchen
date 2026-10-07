using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerProductionApiCollection))]
public sealed class HstsProductionTests
{
    private readonly McpTestServerProductionApiFixture _fixture;

    public HstsProductionTests(McpTestServerProductionApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Production_responses_include_strict_transport_security()
    {
        using var client = _fixture.CreateClient();
        using var response = await client.GetAsync("/api/health");

        response.Headers.Should().ContainKey("Strict-Transport-Security");
        response.Headers.GetValues("Strict-Transport-Security").Should().Equal(SecurityHeaders.StrictTransportSecurity);
    }

    [Fact]
    public async Task Production_responses_forbid_framing_and_sniffing()
    {
        using var client = _fixture.CreateClient();
        using var response = await client.GetAsync("/api/health");

        response.Headers.GetValues("X-Frame-Options").Should().Equal(SecurityHeaders.FrameOptions);
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal(SecurityHeaders.ContentTypeOptions);
        response.Headers.GetValues("Content-Security-Policy").Should().Equal(SecurityHeaders.ContentSecurityPolicy);
    }
}

[Collection(nameof(McpTestServerApiCollection))]
public sealed class HstsDevelopmentTests
{
    private readonly McpTestServerApiFixture _fixture;

    public HstsDevelopmentTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Development_responses_do_not_include_strict_transport_security()
    {
        using var client = _fixture.CreateClient();
        using var response = await client.GetAsync("/api/health");

        response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
    }
}
