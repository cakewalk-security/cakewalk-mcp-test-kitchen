using System.Text.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Tests.Fixtures;
using McpTestServer.API.Tests.Management;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace McpTestServer.API.Tests.Scenarios;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class SdkV2CompatScenarioTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

    private readonly McpTestServerApiFixture _fixture;

    public SdkV2CompatScenarioTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Lists_dedicated_compat_tools()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-list-user@example.com");
        await using var mcpClient = await CreateClientAsync(
            pat,
            McpSessionClient.ProtocolVersion,
            AcceptingCloseReasonHandler("approved"),
            cts.Token);

        var tools = await mcpClient.ListToolsAsync(cancellationToken: cts.Token);
        var names = tools.Select(tool => tool.Name).ToList();
        names.Should().Contain(
        [
            McpToolNames.RunScenario,
            McpToolNames.CloseSupportTicket,
            McpToolNames.ReportCompatContext,
            McpToolNames.GetOrderStatus,
        ]);

        var orderStatus = tools.Single(tool => tool.Name == McpToolNames.GetOrderStatus);
        orderStatus.ProtocolTool.InputSchema.GetRawText().Should().Contain("x-mcp-header");
    }

    [Fact]
    public async Task Run_scenario_returns_compat_matrix()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-index-user@example.com");
        await using var mcpClient = await CreateClientAsync(pat, McpSessionClient.ProtocolVersion, null, cts.Token);

        var result = await mcpClient.CallToolAsync(
            new CallToolRequestParams
            {
                Name = McpToolNames.RunScenario,
                Arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
            },
            cts.Token);

        var text = GetSingleTextResult(result);
        text.Should().Contain("\"scenarioId\":\"compat.sdk_v2\"");
        text.Should().Contain(McpToolNames.CloseSupportTicket);
        text.Should().Contain("\"available\":true");
        text.Should().Contain("sdk-bridge-to-elicitation/create");
    }

    [Fact]
    public async Task Close_support_ticket_succeeds_with_upfront_reason_on_legacy_protocol()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-upfront-legacy@example.com");
        await using var mcpClient = await CreateClientAsync(
            pat,
            McpSessionClient.LegacyProtocolVersion,
            elicitationHandler: null,
            cts.Token);

        var result = await CallCloseTicketAsync(mcpClient, ticketId: 42, closeReason: "done in tests", cts.Token);
        var text = GetSingleTextResult(result);

        text.Should().Contain("\"path\":\"upfront\"");
        text.Should().Contain("\"closeReason\":\"done in tests\"");
        text.Should().Contain("\"ticketId\":42");
    }

    [Fact]
    public async Task Close_support_ticket_returns_guidance_when_legacy_stateless_client_omits_reason()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-guidance-legacy@example.com");
        await using var mcpClient = await CreateClientAsync(
            pat,
            McpSessionClient.LegacyProtocolVersion,
            elicitationHandler: null,
            cts.Token);

        var result = await CallCloseTicketAsync(mcpClient, ticketId: 7, closeReason: null, cts.Token);
        var text = GetSingleTextResult(result);

        text.Should().Contain("\"path\":\"guidance\"");
        text.Should().Contain("Closing a ticket requires a reason");
        text.Should().Contain("2025-11-25/stateless/unsupported");
    }

    [Fact]
    public async Task Close_support_ticket_completes_native_mrtr_when_client_accepts()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-mrtr-accept@example.com");
        await using var mcpClient = await CreateClientAsync(
            pat,
            McpSessionClient.ProtocolVersion,
            AcceptingCloseReasonHandler("closed after review"),
            cts.Token);

        var result = await CallCloseTicketAsync(mcpClient, ticketId: 99, closeReason: null, cts.Token);
        var text = GetSingleTextResult(result);

        text.Should().Contain("\"path\":\"mrtr\"");
        text.Should().Contain("\"closeReason\":\"closed after review\"");
        text.Should().Contain("Closed ticket 99");
    }

    [Fact]
    public async Task Close_support_ticket_cancels_when_elicitation_is_declined()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-mrtr-decline@example.com");
        await using var mcpClient = await CreateClientAsync(
            pat,
            McpSessionClient.ProtocolVersion,
            DecliningHandler(),
            cts.Token);

        var result = await CallCloseTicketAsync(mcpClient, ticketId: 11, closeReason: null, cts.Token);
        var text = GetSingleTextResult(result);

        text.Should().Contain("\"path\":\"cancelled\"");
        text.Should().Contain("Ticket close cancelled");
    }

    [Fact]
    public async Task Report_compat_context_includes_negotiated_protocol_and_headers()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-report@example.com");
        await using var mcpClient = await CreateClientAsync(
            pat,
            McpSessionClient.ProtocolVersion,
            elicitationHandler: null,
            cts.Token);

        var result = await mcpClient.CallToolAsync(
            new CallToolRequestParams
            {
                Name = McpToolNames.ReportCompatContext,
                Arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
            },
            cts.Token);

        var text = GetSingleTextResult(result);
        text.Should().Contain($"\"negotiatedProtocolVersion\":\"{McpSessionClient.ProtocolVersion}\"");
        text.Should().Contain("\"isMrtrSupported\":true");
        text.Should().Contain("\"transportMode\":\"stateless\"");
        text.Should().Contain("Mcp-Method");
    }

    [Fact]
    public async Task Get_order_status_echoes_region_and_order()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var pat = await SelectScenarioAsync("compat-order@example.com");
        await using var mcpClient = await CreateClientAsync(
            pat,
            McpSessionClient.ProtocolVersion,
            elicitationHandler: null,
            cts.Token);

        var result = await mcpClient.CallToolAsync(
            new CallToolRequestParams
            {
                Name = McpToolNames.GetOrderStatus,
                Arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["region"] = JsonSerializer.SerializeToElement("eastus2"),
                    ["orderId"] = JsonSerializer.SerializeToElement("ord-100"),
                },
            },
            cts.Token);

        var text = GetSingleTextResult(result);
        text.Should().Contain("\"region\":\"eastus2\"");
        text.Should().Contain("\"orderId\":\"ord-100\"");
        text.Should().Contain("\"status\":\"ok:eastus2:ord-100\"");
    }

    private async Task<string> SelectScenarioAsync(string email)
    {
        using var managementClient = _fixture.CreateAuthenticatedClient(email);
        await ManagementTestHelpers.SetUserScenarioAsync(
            managementClient,
            ScenarioIds.CompatSdkV2,
            "{}");
        return await ManagementTestHelpers.GetUserPatAsync(managementClient);
    }

    private Task<McpClient> CreateClientAsync(
        string pat,
        string protocolVersion,
        Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>>? elicitationHandler,
        CancellationToken cancellationToken)
    {
        var httpClient = _fixture.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", pat);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Name = "mcp-test-server-sdk-v2-compat-tests",
                Endpoint = new Uri(httpClient.BaseAddress!, McpPaths.McpEndpoint),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            httpClient,
            NullLoggerFactory.Instance,
            ownsHttpClient: false);

        var options = new McpClientOptions
        {
            ProtocolVersion = protocolVersion,
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

    private static ValueTask<CallToolResult> CallCloseTicketAsync(
        McpClient client,
        long ticketId,
        string? closeReason,
        CancellationToken cancellationToken)
    {
        var arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["ticketId"] = JsonSerializer.SerializeToElement(ticketId),
        };
        if (closeReason is not null)
        {
            arguments["closeReason"] = JsonSerializer.SerializeToElement(closeReason);
        }

        return client.CallToolAsync(
            new CallToolRequestParams
            {
                Name = McpToolNames.CloseSupportTicket,
                Arguments = arguments,
            },
            cancellationToken);
    }

    private static Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>> AcceptingCloseReasonHandler(
        string reason) =>
        (_, _) => ValueTask.FromResult(new ElicitResult
        {
            Action = "accept",
            Content = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ "closeReason" ] = JsonSerializer.SerializeToElement(reason),
            },
        });

    private static Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>> DecliningHandler() =>
        (_, _) => ValueTask.FromResult(new ElicitResult { Action = "decline" });

    private static string GetSingleTextResult(CallToolResult result) =>
        result.Content
            .OfType<TextContentBlock>()
            .Select(block => block.Text)
            .FirstOrDefault() ?? string.Empty;
}
