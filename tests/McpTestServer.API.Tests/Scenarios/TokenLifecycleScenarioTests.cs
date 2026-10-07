using FluentAssertions;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Auth;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

public sealed class TokenLifecycleScenarioTests
{
    [Fact]
    public async Task Default_params_reject_the_first_post_then_continue()
    {
        using var session = new ScenarioSession(
            ScenarioIds.AuthTokenLifecycle,
            "{}",
            "token-lifecycle@example.com",
            mcpSessionId: null);
        var scenario = new TokenLifecycleScenario();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;

        var first = await scenario.OnRequestAsync(new ScenarioWireContext
        {
            Session = session,
            HttpContext = httpContext,
            CancellationToken = CancellationToken.None,
        });
        first.Kind.Should().Be(WireDecisionKind.RespondWithStatus);
        first.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var second = await scenario.OnRequestAsync(new ScenarioWireContext
        {
            Session = session,
            HttpContext = httpContext,
            CancellationToken = CancellationToken.None,
        });
        second.Kind.Should().Be(WireDecisionKind.Continue);
    }
}
