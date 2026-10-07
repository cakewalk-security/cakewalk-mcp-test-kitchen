using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ObservationListTests
{
    private readonly McpTestServerApiFixture _fixture;

    public ObservationListTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task List_returns_paginated_response()
    {
        await InitializeSessionForAuthenticatedUserAsync();

        using var managementClient = _fixture.CreateAuthenticatedClient();
        using var response = await managementClient.GetAsync("/api/management/observations?page=1&limit=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Page.Should().Be(1);
        payload.PageSize.Should().Be(2);
        payload.Items.Should().HaveCountLessThanOrEqualTo(2);
        payload.TotalCount.Should().BeGreaterThanOrEqualTo(payload.Items.Count);
    }

    [Fact]
    public async Task List_filters_by_phase()
    {
        await InitializeSessionForAuthenticatedUserAsync();

        using var managementClient = _fixture.CreateAuthenticatedClient();
        using var response = await managementClient.GetAsync(
            $"/api/management/observations?phase={ObservationPhases.Initialize}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().NotBeEmpty();
        payload.Items.Should().OnlyContain(o => o.Phase == ObservationPhases.Initialize);
    }

    [Fact]
    public async Task List_filters_by_method()
    {
        await InitializeSessionForAuthenticatedUserAsync();

        using var managementClient = _fixture.CreateAuthenticatedClient();
        using var response = await managementClient.GetAsync("/api/management/observations?method=initialize");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().NotBeEmpty();
        payload.Items.Should().OnlyContain(o => o.Method == McpMethods.Initialize);
    }

    [Fact]
    public async Task List_filters_by_category()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("observation-category-user@example.com");
        var pat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat!.Pat);
        await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                method = McpMethods.ToolsList,
                @params = new { },
            });
        await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ResourcesList,
                @params = new { },
            });

        using var response = await managementClient.GetAsync("/api/management/observations?category=resources");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().NotBeEmpty();
        payload.Items.Should().OnlyContain(o => o.Method.StartsWith("resources/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task List_clamps_limit_at_max()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        using var response = await managementClient.GetAsync("/api/management/observations?limit=500");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload!.PageSize.Should().Be(ObservationQueryParams.MaxLimit);
    }

    [Fact]
    public async Task List_includes_client_protocol_version_from_request_header()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        var pat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat!.Pat);
        await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                method = McpMethods.ToolsList,
                @params = new { },
            },
            McpSessionClient.LegacyProtocolVersion);

        using var response = await managementClient.GetAsync("/api/management/observations?method=tools/list&limit=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().NotBeEmpty();
        payload.Items[0].ClientProtocolVersion.Should().Be(McpSessionClient.LegacyProtocolVersion);
    }

    private async Task InitializeSessionForAuthenticatedUserAsync()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient();
        var pat = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat!.Pat);
        await mcp.InitializeSessionAsync(protocolVersion: McpSessionClient.LegacyProtocolVersion);
    }
}
