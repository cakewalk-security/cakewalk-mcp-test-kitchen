using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class NewScenarioWireTests
{
    private readonly McpTestServerApiFixture _fixture;

    public NewScenarioWireTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Json_rpc_error_returns_error_envelope_on_tools_call()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("jsonrpc-error-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ErrorsJsonRpcError,
            """{"code":-32602,"message":"Invalid params","onInvocation":1}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ToolsCall,
                @params = new { name = McpToolNames.RunScenario, arguments = new { } },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(HttpMediaTypes.ApplicationJson);
        var document = await McpSessionClient.ReadMcpJsonAsync(response);
        document.RootElement.GetProperty("error").GetProperty("code").GetInt32().Should().Be(-32602);
    }

    [Fact]
    public async Task Tool_error_returns_isError_result()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("tool-error-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ErrorsToolError,
            """{"errorMessage":"Tool-side failure","onInvocation":1}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ToolsCall,
                @params = new { name = McpToolNames.RunScenario, arguments = new { } },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await McpSessionClient.ReadMcpJsonAsync(response);
        document.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().Should().BeTrue();
    }

    [Fact(Skip = ScenarioTestSkipReasons.ScenarioNotRegistered)]
    public async Task Http_status_per_method_returns_401_for_tools_call()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("status-per-method-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ErrorsHttpStatusPerMethod,
            """{"rules":{"tools/call":401,"initialize":200}}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ToolsCall,
                @params = new { name = McpToolNames.RunScenario, arguments = new { } },
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = ScenarioTestSkipReasons.ScenarioNotRegistered)]
    public async Task Token_lifecycle_rejects_then_accepts()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("token-lifecycle-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.AuthTokenLifecycle,
            """{"rejectUntilInvocation":1,"rejectForever":false}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var firstResponse = await McpSessionClient.PostMcpAsync(
            client,
            sessionId: null,
            new
            {
                jsonrpc = "2.0",
                id = 1,
                method = McpMethods.Initialize,
                @params = new
                {
                    protocolVersion = McpSessionClient.LegacyProtocolVersion,
                    capabilities = new { },
                    clientInfo = new { name = "tests", version = "1.0.0" },
                },
            },
            McpSessionClient.LegacyProtocolVersion);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var secondResponse = await McpSessionClient.PostMcpAsync(
            client,
            sessionId: null,
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.Initialize,
                @params = new
                {
                    protocolVersion = McpSessionClient.LegacyProtocolVersion,
                    capabilities = new { },
                    clientInfo = new { name = "tests", version = "1.0.0" },
                },
            },
            McpSessionClient.LegacyProtocolVersion);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = ScenarioTestSkipReasons.ScenarioNotRegistered)]
    public async Task Catalog_pagination_returns_paginated_resources()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("catalog-page-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.CatalogPagination,
            """{"totalItems":5,"pageSize":2,"facet":"resources","fault":"none","faultAtPage":2}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var firstPage = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ResourcesList,
                @params = new { },
            });

        firstPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDocument = await McpSessionClient.ReadMcpJsonAsync(firstPage);
        firstDocument.RootElement.GetProperty("result").GetProperty("resources").GetArrayLength().Should().Be(2);
        firstDocument.RootElement.GetProperty("result").GetProperty("nextCursor").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = ScenarioTestSkipReasons.ScenarioNotRegistered)]
    public async Task Catalog_facet_unavailable_returns_method_not_found()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("catalog-facet-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.CatalogFacetUnavailable,
            """{"unavailableFacets":["resources"]}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ResourcesList,
                @params = new { },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await McpSessionClient.ReadMcpJsonAsync(response);
        document.RootElement.GetProperty("error").GetProperty("code").GetInt32().Should().Be(-32601);
    }

    [Fact(Skip = ScenarioTestSkipReasons.ScenarioNotRegistered)]
    public async Task Cooperative_deadline_honors_delay()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("coop-deadline-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.TimeoutCooperativeDeadline,
            """{"delayMs":300,"honorCancellation":true}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using (await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ToolsCall,
                @params = new { name = McpToolNames.RunScenario, arguments = new { } },
            }))
        {
        }
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(300);
    }

    [Fact(Skip = ScenarioTestSkipReasons.ScenarioNotRegistered)]
    public async Task Malformed_payload_on_tools_call_returns_unparseable_response()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("malformed-payload-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ErrorsMalformedPayload,
            """{"kind":"invalid_json","target":"tool_call","onInvocation":1}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var response = await mcp.PostStatelessAsync(
            new
            {
                jsonrpc = "2.0",
                id = 2,
                method = McpMethods.ToolsCall,
                @params = new { name = McpToolNames.RunScenario, arguments = new { } },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("not valid json");
        raw.Should().NotContain("scenarioId");
    }

    [Fact(Skip = ScenarioTestSkipReasons.ScenarioNotRegistered)]
    public async Task Non_compliant_envelope_escapes_string_request_id_in_json_response()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("non-compliant-envelope-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ErrorsNonCompliantEnvelope,
            """{"httpStatus":400,"resultPayload":"{\"content\":[{\"type\":\"text\",\"text\":\"ok\"}],\"isError\":false}","onInvocation":1}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        const string requestJson =
            """{"jsonrpc":"2.0","id":"quote\"test","method":"tools/call","params":{"name":"run_configured_test_scenario","arguments":{}}}""";

        using var response = await mcp.PostMcpJsonAsync(sessionId: null, requestJson, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var raw = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(raw);
        document.RootElement.GetProperty("id").GetString().Should().Be("quote\"test");
        document.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().Should().BeFalse();
    }
}
