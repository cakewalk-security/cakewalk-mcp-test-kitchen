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
public sealed class RunScenarioTests
{
    private readonly McpTestServerApiFixture _fixture;

    public RunScenarioTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Run_scenario_honours_user_scenario_delay()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("slow-user@example.com");
        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);

        await ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.Baseline,
            """{"slowReportDelayMs":400}""");

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        var callBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new
            {
                name = McpToolNames.RunScenario,
                arguments = new { },
            },
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var callResponse = await mcp.PostStatelessAsync(callBody);
        stopwatch.Stop();

        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(400);
    }
}

[Collection(nameof(McpTestServerApiCollection))]
public sealed class RuntimeStateTests
{
    private readonly McpTestServerApiFixture _fixture;

    public RuntimeStateTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Runtime_returns_invocation_counts_after_tool_calls()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("runtime-user@example.com");
        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        var runBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { message = "x" } },
        };
        using (await mcp.PostStatelessAsync(runBody))
        {
        }

        var runtime = await managementClient.GetFromJsonAsync<RuntimeStateResponse>("/api/management/runtime");
        runtime!.LiveSessions.Should().NotBeEmpty();
        runtime.LiveSessions.First().InvocationCounts[McpToolNames.RunScenario].Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Runtime_reset_clears_invocation_counts()
    {
        using var managementClient = _fixture.CreateAuthenticatedClient("reset-user@example.com");
        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);

        using var client = _fixture.CreateClient();
        var mcp = new McpSessionClient(client, pat);

        var runBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = McpMethods.ToolsCall,
            @params = new { name = McpToolNames.RunScenario, arguments = new { message = "x" } },
        };
        using (await mcp.PostStatelessAsync(runBody))
        {
        }

        await managementClient.PostAsync("/api/management/runtime/reset", content: null);

        var runtime = await managementClient.GetFromJsonAsync<RuntimeStateResponse>("/api/management/runtime");
        runtime!.LiveSessions.First().InvocationCounts.GetValueOrDefault(McpToolNames.RunScenario).Should().Be(0);
    }
}

[Collection(nameof(McpTestServerApiCollection))]
public sealed class HealthAndAccountTests
{
    private readonly McpTestServerApiFixture _fixture;

    public HealthAndAccountTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Health_returns_ok()
    {
        using var client = _fixture.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Account_logout_redirects_to_login_page()
    {
        using var client = _fixture.CreateAuthenticatedClient();
        var response = await client.PostAsync("/Account/Logout", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(LoginPaths.Page);
    }
}

internal static class ManagementTestHelpers
{
    public static async Task<string> GetUserPatAsync(HttpClient managementClient)
    {
        var patResponse = await managementClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");
        patResponse.Should().NotBeNull();
        return patResponse!.Pat;
    }

    public static async Task SetUserScenarioAsync(HttpClient managementClient, string scenarioId, string paramsJson)
    {
        var response = await managementClient.PutAsJsonAsync(
            "/api/management/me/scenario",
            new SetUserScenarioSelectionRequest
            {
                ScenarioId = scenarioId,
                ParamsJson = paramsJson,
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
