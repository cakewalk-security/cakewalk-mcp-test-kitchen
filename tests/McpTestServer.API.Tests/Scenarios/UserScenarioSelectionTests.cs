using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class UserScenarioSelectionTests
{
    private readonly McpTestServerApiFixture _fixture;

    public UserScenarioSelectionTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Put_unknown_scenario_id_returns_400()
    {
        using var client = _fixture.CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync(
            "/api/management/me/scenario",
            new SetUserScenarioSelectionRequest
            {
                ScenarioId = "does-not-exist",
                ParamsJson = "{}",
            });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Two_users_with_different_selections_get_independent_behavior()
    {
        using var userAClient = _fixture.CreateAuthenticatedClient("user-a@example.com");
        using var userBClient = _fixture.CreateAuthenticatedClient("user-b@example.com");

        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            userAClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":300}""");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            userBClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":0}""");

        var patA = await Management.ManagementTestHelpers.GetUserPatAsync(userAClient);
        var patB = await Management.ManagementTestHelpers.GetUserPatAsync(userBClient);

        using var client = _fixture.CreateClient();
        var mcpA = new McpSessionClient(client, patA);
        var mcpB = new McpSessionClient(_fixture.CreateClient(), patB);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { } },
        };

        var stopwatchA = System.Diagnostics.Stopwatch.StartNew();
        using (await mcpA.PostStatelessAsync(callBody))
        {
        }
        stopwatchA.Stop();

        var stopwatchB = System.Diagnostics.Stopwatch.StartNew();
        using (await mcpB.PostStatelessAsync(callBody))
        {
        }
        stopwatchB.Stop();

        stopwatchA.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(300);
        stopwatchB.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task Changing_selection_applies_on_next_call()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("mutable-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":300}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":0}""");

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { } },
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using (await mcp.PostStatelessAsync(callBody))
        {
        }
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task Anonymous_shared_pat_uses_baseline_scenario()
    {
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, _fixture.McpPat);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { message = "anon" } },
        };

        using var callResponse = await mcp.PostStatelessAsync(callBody);
        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Terminate_sessions_allows_new_selection_to_apply()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("terminate-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":300}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":0}""");

        var terminateResponse = await managementClient.PostAsync("/api/management/me/sessions/terminate", content: null);
        terminateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { } },
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using (await mcp.PostStatelessAsync(callBody))
        {
        }
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task Put_null_status_codes_returns_400()
    {
        using var client = _fixture.CreateAuthenticatedClient("null-status-codes@example.com");
        var response = await client.PutAsJsonAsync(
            "/api/management/me/scenario",
            new SetUserScenarioSelectionRequest
            {
                ScenarioId = ScenarioIds.ErrorsHttpStatusSequence,
                ParamsJson = """{"statusCodes":null}""",
            });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Mcp_call_lists_a_registered_session_id()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("session-list-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":0}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);
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

        var sessions = await managementClient.GetFromJsonAsync<ScenarioSessionResponse[]>("/api/management/me/sessions");
        sessions.Should().NotBeNull();
        sessions!.Should().ContainSingle();
        sessions[0].McpSessionId.Should().NotBeNullOrWhiteSpace();
        sessions[0].ScenarioId.Should().Be(ScenarioIds.Baseline);
    }
}

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ScenarioWireTests
{
    private readonly McpTestServerApiFixture _fixture;

    public ScenarioWireTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Http_status_sequence_returns_configured_status_codes()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("status-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ErrorsHttpStatusSequence,
            """{"statusCodes":[503,200]}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        using var firstResponse = await mcp.PostStatelessAsync(
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
        firstResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        using var secondResponse = await mcp.PostStatelessAsync(
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
    public async Task Uncooperative_hang_exceeds_budget_then_next_call_succeeds_quickly()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("hang-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.TimeoutUncooperativeHang,
            """{"hangOnInvocation":1,"hangDurationMs":800}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { } },
        };

        var firstStopwatch = System.Diagnostics.Stopwatch.StartNew();
        using (await mcp.PostStatelessAsync(callBody))
        {
        }
        firstStopwatch.Stop();
        firstStopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(800);

        var secondStopwatch = System.Diagnostics.Stopwatch.StartNew();
        using (await mcp.PostStatelessAsync(callBody))
        {
        }
        secondStopwatch.Stop();
        secondStopwatch.ElapsedMilliseconds.Should().BeLessThan(300);
    }

    [Fact]
    public async Task Observations_include_scenario_id()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("scenario-obs-user@example.com");
        await Management.ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":0}""");

        var pat = await Management.ManagementTestHelpers.GetUserPatAsync(managementClient);
        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { message = "scenario" } },
        };
        using (await mcp.PostStatelessAsync(callBody))
        {
        }

        var payload = await managementClient.GetFromJsonAsync<ObservationListResponse>("/api/management/observations?limit=5");
        payload!.Items.Should().Contain(o => o.ScenarioId == ScenarioIds.Baseline);
    }
}
