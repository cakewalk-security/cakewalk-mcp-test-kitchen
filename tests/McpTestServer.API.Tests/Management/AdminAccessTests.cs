using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class AdminAccessTests
{
    private readonly McpTestServerApiFixture _fixture;

    public AdminAccessTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Users_only_see_their_own_observations()
    {
        using var userAClient = _fixture.CreateAuthenticatedClient("alice@example.com");
        var patA = await userAClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var mcpClientA = _fixture.CreateClient();
        var mcpA = new McpSessionClient(mcpClientA, patA!.Pat);
        await mcpA.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        using var userBClient = _fixture.CreateAuthenticatedClient("bob@example.com");
        var patB = await userBClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var mcpClientB = _fixture.CreateClient();
        var mcpB = new McpSessionClient(mcpClientB, patB!.Pat);
        await mcpB.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        var alicePayload = await userAClient.GetFromJsonAsync<ObservationListResponse>("/api/management/observations");
        alicePayload!.Items.Should().NotBeEmpty();
        alicePayload.Items.Should().OnlyContain(o => o.CallerEmail == "alice@example.com");

        var bobPayload = await userBClient.GetFromJsonAsync<ObservationListResponse>("/api/management/observations");
        bobPayload!.Items.Should().NotBeEmpty();
        bobPayload.Items.Should().OnlyContain(o => o.CallerEmail == "bob@example.com");
    }
}
