using System.Text.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Scenarios.Elicitation;
using McpTestServer.API.Tests.Fixtures;
using McpTestServer.API.Tests.Management;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class ElicitationApprovalScenarioTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

    private readonly McpTestServerApiFixture _fixture;

    public ElicitationApprovalScenarioTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Run_scenario_elicitation_accepted_when_client_supports_form_mode()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        using var managementClient = _fixture.CreateAuthenticatedClient("elicitation-accept-user@example.com");
        await ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ElicitationApproval,
            "{}");

        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);
        await using var mcpClient = await CreateMcpClientAsync(
            pat,
            AcceptingHandlerReturningReason("approved for audit"),
            cts.Token);

        var result = await mcpClient.CallToolAsync(
            new CallToolRequestParams
            {
                Name = McpToolNames.RunScenario,
                Arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
            },
            cts.Token);

        var text = GetSingleTextResult(result);
        text.Should().Contain("\"elicitationOutcome\":\"accepted\"");
        text.Should().Contain("\"approvalReason\":\"approved for audit\"");
    }

    [Fact]
    public async Task Run_scenario_elicitation_unsupported_when_client_does_not_advertise_capability()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        using var managementClient = _fixture.CreateAuthenticatedClient("elicitation-unsupported-user@example.com");
        await ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.ElicitationApproval,
            "{}");

        var pat = await ManagementTestHelpers.GetUserPatAsync(managementClient);
        await using var mcpClient = await CreateMcpClientAsync(
            pat,
            elicitationHandler: null,
            cts.Token);

        Func<Task> act = async () => await mcpClient.CallToolAsync(
            new CallToolRequestParams
            {
                Name = McpToolNames.RunScenario,
                Arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
            },
            cts.Token);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private Task<McpClient> CreateMcpClientAsync(
        string pat,
        Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>>? elicitationHandler,
        CancellationToken cancellationToken)
    {
        var httpClient = _fixture.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", pat);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Name = "mcp-test-server-elicitation-tests",
                Endpoint = new Uri(httpClient.BaseAddress!, McpPaths.McpEndpoint),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            httpClient,
            NullLoggerFactory.Instance,
            ownsHttpClient: false);

        var options = new McpClientOptions
        {
            Capabilities = new ClientCapabilities
            {
                Elicitation = elicitationHandler is null
                    ? null
                    : new ElicitationCapability { Form = new FormElicitationCapability() },
            },
            Handlers = new McpClientHandlers
            {
                ElicitationHandler = elicitationHandler,
            },
        };

        return McpClient.CreateAsync(transport, options, NullLoggerFactory.Instance, cancellationToken);
    }

    private static Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>> AcceptingHandlerReturningReason(
        string reason) =>
        (_, _) => ValueTask.FromResult(new ElicitResult
        {
            Action = "accept",
            Content = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ElicitationApprovalScenario.ReasonFieldName] = JsonSerializer.SerializeToElement(reason),
            },
        });

    private static string GetSingleTextResult(CallToolResult result) =>
        result.Content
            .OfType<TextContentBlock>()
            .Select(block => block.Text)
            .FirstOrDefault() ?? string.Empty;
}
