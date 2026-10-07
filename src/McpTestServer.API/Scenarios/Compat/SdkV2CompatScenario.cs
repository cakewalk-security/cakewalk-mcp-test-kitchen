using System.Text.Json;
using McpTestServer.API.Constants;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Compat;

public sealed class SdkV2CompatScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.CompatSdkV2,
        "C# SDK v2 backward compatibility",
        $"This host is configured with {McpTransportConstants.StatelessLabel} (no MCP sessions). " +
        "Dedicated tools exercise official MCP C# SDK 2.0 / 2026-07-28 backward-compat paths: " +
        "simulate_ticket_close_mrtr (up-front argument, native MRTR, or down-level guidance), " +
        "show_negotiated_mcp_protocol (negotiated protocol and headers), and verify_order_region_param_header (Mcp-Param header promotion). " +
        "run_configured_test_scenario returns a matrix of which cells this stateless host can cover.",
        ScenarioAreas.Compat,
        """
        {
          "defaultCloseReason": "completed",
          "promptMessage": "Close ticket '{ticketId}'? Accept the default reason or provide your own."
        }
        """,
        McpCsharpSdkV2Article.Url);

    public override void ConfigureSession(McpServerOptions options, ScenarioSession session)
    {
        var tools = new SdkV2CompatTools(session);
        options.ToolCollection = new McpServerPrimitiveCollection<McpServerTool>
        {
            new RunScenarioMcpServerTool(session, this),
            McpServerTool.Create(tools.CloseSupportTicket),
            McpServerTool.Create(tools.ReportCompatContext),
            McpServerTool.Create(tools.GetOrderStatus),
        };
        ScenarioSessionSetup.ApplyBaselineCatalog(options);
    }

    public override ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var payload = new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["invocation"] = invocation,
            ["message"] = ReadOptionalMessage(arguments),
            ["transportMode"] = "stateless",
            ["purpose"] =
                "Exercise MCP C# SDK v2.0 backward-compat paths from the 2026-07-28 spec. " +
                "Call the dedicated tools listed here rather than this index.",
            ["tools"] = new object[]
            {
                new
                {
                    name = McpToolNames.CloseSupportTicket,
                    covers = "Four-client MRTR matrix from the SDK v2 announcement (up-front closeReason, native MRTR, cancelled elicitation, session-less down-level guidance).",
                },
                new
                {
                    name = McpToolNames.ReportCompatContext,
                    covers = "Negotiated protocol version, IsMrtrSupported, session id, and MCP HTTP headers for the current request.",
                },
                new
                {
                    name = McpToolNames.GetOrderStatus,
                    covers = "SEP-2243 header promotion: region is advertised as x-mcp-header and mirrored to Mcp-Param-Region.",
                },
            },
            ["compatMatrix"] = new object[]
            {
                new
                {
                    protocol = McpProtocolConstants.LatestSupportedProtocolVersion,
                    session = "stateless",
                    mrtr = "native",
                    available = true,
                },
                new
                {
                    protocol = McpProtocolConstants.LatestSupportedProtocolVersion,
                    session = "stateful",
                    mrtr = "native",
                    available = false,
                    note = "This host hard-codes HttpServerTransportOptions.Stateless = true.",
                },
                new
                {
                    protocol = "2025-11-25",
                    session = "stateful",
                    mrtr = "sdk-bridge-to-elicitation/create",
                    available = false,
                    note = "Requires opt-in sessions; not enabled on this host.",
                },
                new
                {
                    protocol = "2025-11-25",
                    session = "stateless",
                    mrtr = "unsupported",
                    available = true,
                    note = "simulate_ticket_close_mrtr returns guidance to resend with closeReason.",
                },
            },
        };

        return ValueTask.FromResult(SdkV2CompatTools.JsonResult(payload));
    }
}
