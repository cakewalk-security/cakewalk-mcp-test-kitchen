using System.ComponentModel;
using System.Text.Json;
using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios.Elicitation;
using McpTestServer.Infrastructure.Constants;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Compat;

internal sealed class SdkV2CompatTools(ScenarioSession session)
{
    public const string CloseReasonFieldName = "closeReason";

    public const string RegionHeaderName = "Region";

    [McpServerTool(Name = McpToolNames.CloseSupportTicket),
     Description(
         "MCP Test Kitchen fixture. Simulates closing a support ticket to exercise the MCP C# SDK v2 MRTR " +
         "backward-compat matrix: up-front closeReason, native 2026-07-28 elicitation, or down-level guidance to resend.")]
    public CallToolResult CloseSupportTicket(
        McpServer server,
        RequestContext<CallToolRequestParams> context,
        [Description("The ID of the ticket to close")] long ticketId,
        [Description("Why the ticket is being closed. Provide this up-front to skip elicitation.")]
        string? closeReason = null)
    {
        session.IncrementInvocationCount(McpToolNames.CloseSupportTicket);
        var parameters = session.GetParams<SdkV2CompatParams>();
        var defaultCloseReason = string.IsNullOrWhiteSpace(parameters.DefaultCloseReason)
            ? SdkV2CompatParams.FallbackCloseReason
            : parameters.DefaultCloseReason;

        var confirmedReason = closeReason;
        if (string.IsNullOrWhiteSpace(confirmedReason)
            && ElicitationMrtrHelper.TryGetElicitResult(context, out var reasonResult, CloseReasonFieldName))
        {
            if (reasonResult?.IsAccepted is not true)
            {
                return JsonResult(new Dictionary<string, object?>
                {
                    ["scenarioId"] = session.ScenarioId,
                    ["tool"] = McpToolNames.CloseSupportTicket,
                    ["path"] = SdkV2CompatPaths.Cancelled,
                    ["ticketId"] = ticketId,
                    ["isMrtrSupported"] = server.IsMrtrSupported,
                    ["negotiatedProtocolVersion"] = server.NegotiatedProtocolVersion,
                    ["transportMode"] = "stateless",
                    ["message"] = "Ticket close cancelled",
                });
            }

            confirmedReason = reasonResult.Content?.TryGetValue(CloseReasonFieldName, out var reasonValue) is true
                ? reasonValue.GetString()
                : null;
            confirmedReason = string.IsNullOrWhiteSpace(confirmedReason) ? defaultCloseReason : confirmedReason;

            return ClosedResult(
                server,
                ticketId,
                confirmedReason,
                SdkV2CompatPaths.Mrtr);
        }

        if (!string.IsNullOrWhiteSpace(confirmedReason))
        {
            return ClosedResult(server, ticketId, confirmedReason, SdkV2CompatPaths.Upfront);
        }

        if (server.IsMrtrSupported)
        {
            var prompt = parameters.PromptMessage.Replace("{ticketId}", ticketId.ToString(), StringComparison.Ordinal);
            ElicitationMrtrHelper.ThrowElicitation(
                new ElicitRequestParams
                {
                    Mode = McpElicitationModes.Form,
                    Message = prompt,
                    RequestedSchema = new()
                    {
                        Properties =
                        {
                            [CloseReasonFieldName] = new ElicitRequestParams.StringSchema
                            {
                                Title = "Close reason",
                                Description = "The reason for closing the ticket",
                                Default = defaultCloseReason,
                            },
                        },
                    },
                },
                requestState: ticketId.ToString(),
                inputResponseKey: CloseReasonFieldName);
        }

        return JsonResult(new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["tool"] = McpToolNames.CloseSupportTicket,
            ["path"] = SdkV2CompatPaths.Guidance,
            ["ticketId"] = ticketId,
            ["isMrtrSupported"] = server.IsMrtrSupported,
            ["negotiatedProtocolVersion"] = server.NegotiatedProtocolVersion,
            ["transportMode"] = "stateless",
            ["compatCell"] = DescribeCompatCell(server),
            ["message"] = "Closing a ticket requires a reason. Resend with `closeReason`.",
        });
    }

    [McpServerTool(Name = McpToolNames.ReportCompatContext),
     Description(
         "MCP Test Kitchen fixture. Returns the negotiated MCP protocol version, MRTR support, session id, " +
         "and MCP HTTP headers so clients can see which SDK v2 compat-matrix cell they are in.")]
    public CallToolResult ReportCompatContext(
        McpServer server,
        RequestContext<CallToolRequestParams> context)
    {
        session.IncrementInvocationCount(McpToolNames.ReportCompatContext);
        var headers = ReadMcpHeaders(context);

        return JsonResult(new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["tool"] = McpToolNames.ReportCompatContext,
            ["negotiatedProtocolVersion"] = server.NegotiatedProtocolVersion,
            ["isJuly2026OrLater"] = IsJuly2026OrLater(server.NegotiatedProtocolVersion),
            ["isMrtrSupported"] = server.IsMrtrSupported,
            ["sessionId"] = server.SessionId,
            ["transportMode"] = "stateless",
            ["compatCell"] = DescribeCompatCell(server),
            ["headers"] = headers,
            ["note"] =
                "This host is Stateless=true. Native MRTR (2026-07-28) and the session-less down-level " +
                "guidance path are available. The SDK's stateful elicitation/create bridge is not, because " +
                "sessions are not enabled.",
        });
    }

    [McpServerTool(Name = McpToolNames.GetOrderStatus),
     Description(
         "MCP Test Kitchen fixture. Simulates an order lookup to verify region is promoted to an Mcp-Param-Region header.")]
    public CallToolResult GetOrderStatus(
        RequestContext<CallToolRequestParams> context,
        [McpHeader(RegionHeaderName), Description("Orders service region")] string region,
        [Description("The order to look up")] string orderId)
    {
        session.IncrementInvocationCount(McpToolNames.GetOrderStatus);
        var receivedHeader = ReadHeader(context, McpProtocolHeaders.ParamPrefix + RegionHeaderName);

        return JsonResult(new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["tool"] = McpToolNames.GetOrderStatus,
            ["region"] = region,
            ["orderId"] = orderId,
            ["receivedParamHeader"] = receivedHeader,
            ["headerMatchesBody"] = string.Equals(receivedHeader, region, StringComparison.Ordinal),
            ["status"] = $"ok:{region}:{orderId}",
        });
    }

    private CallToolResult ClosedResult(
        McpServer server,
        long ticketId,
        string closeReason,
        string path) =>
        JsonResult(new Dictionary<string, object?>
        {
            ["scenarioId"] = session.ScenarioId,
            ["tool"] = McpToolNames.CloseSupportTicket,
            ["path"] = path,
            ["ticketId"] = ticketId,
            ["closeReason"] = closeReason,
            ["isMrtrSupported"] = server.IsMrtrSupported,
            ["negotiatedProtocolVersion"] = server.NegotiatedProtocolVersion,
            ["transportMode"] = "stateless",
            ["compatCell"] = DescribeCompatCell(server),
            ["message"] = $"Closed ticket {ticketId}: {closeReason}",
        });

    internal static string DescribeCompatCell(McpServer server)
    {
        var protocol = string.IsNullOrWhiteSpace(server.NegotiatedProtocolVersion)
            ? "unknown"
            : server.NegotiatedProtocolVersion;
        var mrtr = server.IsMrtrSupported ? "native" : "unsupported";
        return $"{protocol}/stateless/{mrtr}";
    }

    internal static Dictionary<string, string?> ReadMcpHeaders(RequestContext<CallToolRequestParams> context)
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [McpProtocolHeaders.ProtocolVersion] = ReadHeader(context, McpProtocolHeaders.ProtocolVersion),
            [McpProtocolHeaders.Method] = ReadHeader(context, McpProtocolHeaders.Method),
            [McpProtocolHeaders.Name] = ReadHeader(context, McpProtocolHeaders.Name),
            [AuthHeaderNames.McpSessionId] = ReadHeader(context, AuthHeaderNames.McpSessionId),
        };
    }

    internal static bool IsJuly2026OrLater(string? protocolVersion) =>
        !string.IsNullOrWhiteSpace(protocolVersion)
        && string.CompareOrdinal(protocolVersion, McpProtocolConstants.LatestSupportedProtocolVersion) >= 0;

    internal static string? ReadHeader(RequestContext<CallToolRequestParams> context, string headerName)
    {
        var httpContext = context.Server.Services?.GetService<IHttpContextAccessor>()?.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        return httpContext.Request.Headers.TryGetValue(headerName, out var values)
            ? values.ToString()
            : null;
    }

    internal static CallToolResult JsonResult(Dictionary<string, object?> payload) =>
        new()
        {
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(payload),
                },
            ],
        };
}
