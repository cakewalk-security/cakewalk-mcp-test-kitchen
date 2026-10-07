using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Mcp;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class McpRequestBodySizeTests
{
    private readonly McpTestServerApiFixture _fixture;

    public McpRequestBodySizeTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Post_larger_than_the_request_cap_returns_413()
    {
        using var client = _fixture.CreateClient();
        using var content = new ByteArrayContent(new byte[McpObservationConstants.MaxRequestBodyBytes + 1]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await client.PostAsync(McpPaths.McpEndpoint, content);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }
}
