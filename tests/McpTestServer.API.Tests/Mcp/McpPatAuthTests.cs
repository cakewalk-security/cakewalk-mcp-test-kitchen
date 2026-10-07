using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using McpTestServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpTestServer.API.Tests.Mcp;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class McpPatAuthTests
{
    private readonly McpTestServerApiFixture _fixture;

    public McpPatAuthTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Mcp_without_pat_returns_401()
    {
        using var client = _fixture.CreateClient();
        var response = await client.PostAsync(McpPaths.McpEndpoint, JsonContent.Create(new { }));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Mcp_with_invalid_pat_returns_401()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "wrong-token");
        var response = await client.PostAsync(McpPaths.McpEndpoint, JsonContent.Create(new { }));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Mcp_rejected_request_does_not_record_observation()
    {
        var method = $"unauthenticated/{Guid.NewGuid():N}";
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "wrong-token");

        var response = await client.PostAsync(
            McpPaths.McpEndpoint,
            JsonContent.Create(new { jsonrpc = "2.0", id = 1, method }));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpTestServerContext>();
        (await db.Observations.AnyAsync(o => o.Method == method)).Should().BeFalse();
    }

    [Fact]
    public async Task Mcp_with_valid_pat_run_scenario_returns_scenario_payload()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new
            {
                name = McpToolNames.RunScenario,
                arguments = new { message = "hello" },
            },
        };

        using var callResponse = await mcp.PostStatelessAsync(callBody);
        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = await McpSessionClient.ReadMcpJsonAsync(callResponse);
        var content = doc.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString();
        content.Should().Contain("\"scenarioId\":\"baseline\"");
        content.Should().Contain("\"message\":\"hello\"");
    }

    [Fact]
    public async Task Mcp_malformed_request_records_observation_for_authenticated_caller()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        var pat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", pat!.Pat);
        var response = await client.PostAsync(McpPaths.McpEndpoint, JsonContent.Create(new { }));
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        using var observationsResponse = await managementClient.GetAsync("/api/management/observations?limit=5");
        observationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await observationsResponse.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().Contain(o =>
            o.Phase == ObservationPhases.Malformed &&
            o.CallerEmail == "test-user@example.com");
    }
}

[Collection(nameof(McpTestServerProductionApiCollection))]
public sealed class McpPatAuthProductionTests
{
    private readonly McpTestServerProductionApiFixture _fixture;

    public McpPatAuthProductionTests(McpTestServerProductionApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Mcp_without_pat_returns_401_outside_development()
    {
        using var client = _fixture.CreateClient();
        var response = await client.PostAsync(McpPaths.McpEndpoint, JsonContent.Create(new { }));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
