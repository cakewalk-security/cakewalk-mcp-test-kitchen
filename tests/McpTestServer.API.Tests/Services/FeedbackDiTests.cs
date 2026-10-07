using FluentAssertions;
using McpTestServer.API.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Resend;
using Xunit;

namespace McpTestServer.API.Tests.Services;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class FeedbackDiTests
{
    private readonly McpTestServerApiFixture _fixture;

    public FeedbackDiTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public void IResend_is_resolvable_from_the_host()
    {
        using var scope = _fixture.Services.CreateScope();
        var resend = scope.ServiceProvider.GetRequiredService<IResend>();
        resend.Should().NotBeNull();
    }
}
