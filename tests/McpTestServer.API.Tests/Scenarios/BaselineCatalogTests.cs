using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Baseline;
using McpTestServer.API.Tests.Fixtures;
using McpTestServer.API.Tests.Management;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class BaselineCatalogTests
{
    private readonly McpTestServerApiFixture _fixture;

    public BaselineCatalogTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Resources_list_includes_valid_and_invalid_catalog_items()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 1,
                method = McpMethods.ResourcesList,
                @params = new { },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await McpSessionClient.ReadMcpJsonAsync(response);
        var resources = document.RootElement.GetProperty("result").GetProperty("resources");
        var names = resources.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToList();
        names.Should().Contain(
        [
            McpResourceNames.ValidResource,
            McpResourceNames.InvalidResource,
        ]);
    }

    [Fact]
    public async Task Prompts_list_includes_valid_and_invalid_catalog_items()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.PromptsList,
                @params = new { },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await McpSessionClient.ReadMcpJsonAsync(response);
        var prompts = document.RootElement.GetProperty("result").GetProperty("prompts");
        var names = prompts.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToList();
        names.Should().Contain(
        [
            McpPromptNames.ValidPrompt,
            McpPromptNames.InvalidPrompt,
        ]);
    }

    [Fact]
    public async Task Valid_resource_read_returns_text_contents()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 3,
                method = McpMethods.ResourcesRead,
                @params = new { uri = McpResourceUris.Valid },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await McpSessionClient.ReadMcpJsonAsync(response);
        var contents = document.RootElement.GetProperty("result").GetProperty("contents");
        contents.GetArrayLength().Should().BeGreaterThan(0);
        contents[0].GetProperty("text").GetString().Should().Be(BaselineCatalogPrimitives.ValidResourceText);
    }

    [Fact]
    public async Task Valid_prompt_get_returns_user_message()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 4,
                method = McpMethods.PromptsGet,
                @params = new
                {
                    name = McpPromptNames.ValidPrompt,
                    arguments = new { topic = "gateway" },
                },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await McpSessionClient.ReadMcpJsonAsync(response);
        var messages = document.RootElement.GetProperty("result").GetProperty("messages");
        messages.GetArrayLength().Should().BeGreaterThan(0);
        messages[0].GetProperty("role").GetString().Should().Be("user");
        messages[0].GetProperty("content").GetProperty("text").GetString()
            .Should().Contain("Topic: gateway");
    }

    [Fact]
    public async Task Invalid_resource_read_returns_schema_invalid_result()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 5,
                method = McpMethods.ResourcesRead,
                @params = new { uri = McpResourceUris.Invalid },
            },
            McpSessionClient.ProtocolVersion);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await McpSessionClient.ReadMcpJsonAsync(response);
        document.RootElement.TryGetProperty("error", out _).Should().BeFalse();
        var content = document.RootElement.GetProperty("result").GetProperty("contents")[0];
        content.GetProperty("type").GetString().Should().Be("blob");
        content.GetProperty("blob").GetString().Should().Be("not-valid-base64!!!");
        content.GetProperty("mimeType").GetString().Should().Be("not-a-mime-type");
        content.TryGetProperty("uri", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_prompt_get_returns_schema_invalid_result()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 6,
                method = McpMethods.PromptsGet,
                @params = new { name = McpPromptNames.InvalidPrompt },
            },
            McpSessionClient.ProtocolVersion);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await McpSessionClient.ReadMcpJsonAsync(response);
        document.RootElement.TryGetProperty("error", out _).Should().BeFalse();
        var message = document.RootElement.GetProperty("result").GetProperty("messages")[0];
        message.TryGetProperty("role", out _).Should().BeFalse();
        message.GetProperty("content").GetProperty("type").GetString().Should().Be("unknown_type");
    }

    [Fact]
    public async Task Compat_scenario_still_exposes_baseline_catalog_items()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("baseline-catalog-compat@example.com");
        await ManagementTestHelpers.SetUserScenarioAsync(managementClient, ScenarioIds.CompatSdkV2, "{}");
        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var resourcesResponse = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 7,
                method = McpMethods.ResourcesList,
                @params = new { },
            });

        using var resourcesDocument = await McpSessionClient.ReadMcpJsonAsync(resourcesResponse);
        var resourceNames = resourcesDocument.RootElement
            .GetProperty("result")
            .GetProperty("resources")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString())
            .ToList();
        resourceNames.Should().Contain(McpResourceNames.ValidResource);

        using var promptsResponse = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 8,
                method = McpMethods.PromptsList,
                @params = new { },
            });

        using var promptsDocument = await McpSessionClient.ReadMcpJsonAsync(promptsResponse);
        var promptNames = promptsDocument.RootElement
            .GetProperty("result")
            .GetProperty("prompts")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString())
            .ToList();
        promptNames.Should().Contain(McpPromptNames.ValidPrompt);
    }

    [Fact]
    public async Task Resource_read_observation_does_not_include_scenario_id()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("baseline-catalog-obs-user@example.com");
        await ManagementTestHelpers.SetUserScenarioAsync(managementClient, ScenarioIds.CompatSdkV2, "{}");
        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);
        using var readResponse = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 9,
                method = McpMethods.ResourcesRead,
                @params = new { uri = McpResourceUris.Valid },
            });
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var observationsResponse = await managementClient.GetAsync(
            "/api/management/observations?method=resources/read&limit=1");
        observationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await observationsResponse.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().NotBeEmpty();
        payload.Items[0].ScenarioId.Should().BeNull();
    }

    [Fact]
    public async Task Tool_call_observation_still_includes_scenario_id()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("baseline-catalog-tool-obs@example.com");
        await ManagementTestHelpers.SetUserScenarioAsync(managementClient, ScenarioIds.CompatSdkV2, "{}");
        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);
        using var callResponse = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 10,
                method = McpMethods.ToolsCall,
                @params = new { name = McpToolNames.RunScenario, arguments = new { } },
            });
        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var observationsResponse = await managementClient.GetAsync(
            "/api/management/observations?method=tools/call&limit=1");
        observationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await observationsResponse.Content.ReadFromJsonAsync<ObservationListResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().NotBeEmpty();
        payload.Items[0].ScenarioId.Should().Be(ScenarioIds.CompatSdkV2);
    }
}
