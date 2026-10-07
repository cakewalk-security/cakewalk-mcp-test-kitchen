using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class UserMcpPatTests
{
    private readonly McpTestServerApiFixture _fixture;

    public UserMcpPatTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task User_pat_run_scenario_records_caller_email_on_observation()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        using var patResponse = await managementClient.GetAsync("/api/management/me/pat");
        patResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var patDoc = await System.Text.Json.JsonDocument.ParseAsync(await patResponse.Content.ReadAsStreamAsync());
        var pat = patDoc.RootElement.GetProperty("pat").GetString();
        pat.Should().NotBeNullOrWhiteSpace();

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat!);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new
            {
                name = McpToolNames.RunScenario,
                arguments = new { message = "pat-user" },
            },
        };

        using var callResponse = await mcp.PostStatelessAsync(callBody);
        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var observationsResponse = await managementClient.GetAsync("/api/management/observations?limit=5");
        observationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await observationsResponse.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(o =>
            o.Method == McpMethods.ToolsCall &&
            o.Phase == ObservationPhases.ToolExecution &&
            o.CallerEmail == "test-user@example.com");
    }

    [Fact]
    public async Task Mcp_initialize_records_initialize_and_client_initialized_observations()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        var pat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat!.Pat);
        await mcp.InitializeSessionAsync(protocolVersion: McpSessionClient.LegacyProtocolVersion);

        using var observationsResponse = await managementClient.GetAsync("/api/management/observations?limit=10");
        observationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await observationsResponse.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(o =>
            o.Method == McpMethods.Initialize &&
            o.Phase == ObservationPhases.Initialize);
        payload.Items.Should().Contain(o =>
            o.Method == McpMethods.NotificationsInitialized &&
            o.Phase == ObservationPhases.ClientInitialized);
    }

    [Fact]
    public async Task Mcp_tools_list_records_discovery_observation()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        var pat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat!.Pat);

        var listBody = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = McpMethods.ToolsList,
            @params = new { },
        };

        using var listResponse = await mcp.PostStatelessAsync(listBody);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var observationsResponse = await managementClient.GetAsync("/api/management/observations?limit=10");
        observationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await observationsResponse.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(o =>
            o.Method == McpMethods.ToolsList &&
            o.Phase == ObservationPhases.Discovery);
    }

    [Fact]
    public async Task Pat_regeneration_invalidates_old_token()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        var firstPat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");
        firstPat.Should().NotBeNull();

        var regenerateResponse = await managementClient.PostAsync("/api/management/me/pat/regenerate", content: null);
        regenerateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstPat!.Pat);
        var response = await client.PostAsync(McpPaths.McpEndpoint, JsonContent.Create(new { }));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
