using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Options;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class FeedbackControllerTests
{
    private readonly McpTestServerApiFixture _fixture;

    public FeedbackControllerTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Submit_rejects_messages_longer_than_the_cap()
    {
        using var client = _fixture.CreateAuthenticatedClient("feedback-cap@example.com");
        using var response = await client.PostAsJsonAsync(
            "/api/management/feedback",
            new { message = new string('a', FeedbackOptions.MaxMessageLength + 1) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_accepts_a_message_at_the_cap_when_resend_is_unconfigured()
    {
        using var client = _fixture.CreateAuthenticatedClient("feedback-ok@example.com");
        using var response = await client.PostAsJsonAsync(
            "/api/management/feedback",
            new { message = new string('a', FeedbackOptions.MaxMessageLength) });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
