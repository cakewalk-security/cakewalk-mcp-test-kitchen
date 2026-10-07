using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ObservationClearTests
{
    private readonly McpTestServerApiFixture _fixture;

    public ObservationClearTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Clear_deletes_only_current_user_observations()
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

        using var clearResponse = await userAClient.DeleteAsync("/api/management/observations");
        clearResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var alicePayload = await userAClient.GetFromJsonAsync<ObservationListResponse>("/api/management/observations");
        alicePayload!.TotalCount.Should().Be(0);
        alicePayload.Items.Should().BeEmpty();

        var bobPayload = await userBClient.GetFromJsonAsync<ObservationListResponse>("/api/management/observations");
        bobPayload!.TotalCount.Should().BeGreaterThan(0);
        bobPayload.Items.Should().OnlyContain(o => o.CallerEmail == "bob@example.com");
    }
}
