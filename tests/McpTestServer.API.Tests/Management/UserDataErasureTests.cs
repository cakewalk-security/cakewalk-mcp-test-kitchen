using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class UserDataErasureTests
{
    private readonly McpTestServerApiFixture _fixture;

    public UserDataErasureTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Erase_rejects_wrong_confirmation_and_keeps_data()
    {
        using var client = _fixture.CreateAuthenticatedClient("erase-reject@example.com");
        var pat = await ManagementTestHelpers.GetUserPatAsync(client);
        await ManagementTestHelpers.SetUserScenarioAsync(
            client,
            ScenarioIds.ErrorsJsonRpcError,
            """{"code":-32602,"message":"Invalid params","onInvocation":1}""");

        using var mcpClient = _fixture.CreateClient();
        var mcp = new McpSessionClient(mcpClient, pat);
        await mcp.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        using var response = await client.PostAsJsonAsync(
            "/api/management/me/erase",
            new EraseUserDataRequest { Confirmation = "please delete" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var observations = await client.GetFromJsonAsync<ObservationListResponse>("/api/management/observations");
        observations!.TotalCount.Should().BeGreaterThan(0);

        var selection = await client.GetFromJsonAsync<UserScenarioSelectionResponse>("/api/management/me/scenario");
        selection!.ScenarioId.Should().Be(ScenarioIds.ErrorsJsonRpcError);

        using var stillValid = await mcp.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsList,
            @params = new { },
        });
        stillValid.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Erase_deletes_only_current_user_data()
    {
        using var userAClient = _fixture.CreateAuthenticatedClient("erase-alice@example.com");
        using var userBClient = _fixture.CreateAuthenticatedClient("erase-bob@example.com");

        var patA = await ManagementTestHelpers.GetUserPatAsync(userAClient);
        var patB = await ManagementTestHelpers.GetUserPatAsync(userBClient);

        await ManagementTestHelpers.SetUserScenarioAsync(
            userAClient,
            ScenarioIds.ErrorsJsonRpcError,
            """{"code":-32602,"message":"Invalid params","onInvocation":1}""");
        await ManagementTestHelpers.SetUserScenarioAsync(
            userBClient,
            ScenarioIds.ErrorsToolError,
            """{"errorMessage":"boom","onInvocation":1}""");

        using var mcpClientA = _fixture.CreateClient();
        var mcpA = new McpSessionClient(mcpClientA, patA);
        await mcpA.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        using var mcpClientB = _fixture.CreateClient();
        var mcpB = new McpSessionClient(mcpClientB, patB);
        await mcpB.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        using var eraseResponse = await userAClient.PostAsJsonAsync(
            "/api/management/me/erase",
            new EraseUserDataRequest { Confirmation = UserDataErasureConstants.ConfirmationPhrase });
        eraseResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var aliceObservations = await userAClient.GetFromJsonAsync<ObservationListResponse>(
            "/api/management/observations");
        aliceObservations!.TotalCount.Should().Be(0);
        aliceObservations.Items.Should().BeEmpty();

        var aliceSelection = await userAClient.GetFromJsonAsync<UserScenarioSelectionResponse>(
            "/api/management/me/scenario");
        aliceSelection!.ScenarioId.Should().Be(ScenarioIds.Baseline);

        using var revokedPat = await mcpA.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsList,
            @params = new { },
        });
        revokedPat.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var bobObservations = await userBClient.GetFromJsonAsync<ObservationListResponse>(
            "/api/management/observations");
        bobObservations!.TotalCount.Should().BeGreaterThan(0);
        bobObservations.Items.Should().OnlyContain(o => o.CallerEmail == "erase-bob@example.com");

        var bobSelection = await userBClient.GetFromJsonAsync<UserScenarioSelectionResponse>(
            "/api/management/me/scenario");
        bobSelection!.ScenarioId.Should().Be(ScenarioIds.ErrorsToolError);

        using var bobStillValid = await mcpB.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsList,
            @params = new { },
        });
        bobStillValid.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
