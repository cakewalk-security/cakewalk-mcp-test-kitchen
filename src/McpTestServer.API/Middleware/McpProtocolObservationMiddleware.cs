using System.Diagnostics;
using System.Text.Json;
using McpTestServer.API.Constants;
using McpTestServer.API.Mcp;
using McpTestServer.API.Scenarios;
using McpTestServer.API.Services.Observations;

namespace McpTestServer.API.Middleware;

public sealed class McpProtocolObservationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IObservationRecorder observationRecorder)
    {
        if (!IsMcpPost(context))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering(
            bufferThreshold: McpObservationConstants.RequestBodyBufferLimitBytes,
            bufferLimit: McpObservationConstants.RequestBodyBufferLimitBytes);

        var requestSummary = await TryReadJsonRpcRequestAsync(context);
        if (ScenarioWireRequestReader.IsWireBodyTooLarge(context))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = HttpMediaTypes.ApplicationJson;
            await context.Response.WriteAsync(
                ScenarioWireJsonRpc.Error(null, McpJsonRpcErrorCodes.ParseError, McpJsonRpcErrorMessages.ParseError),
                context.RequestAborted);
            await observationRecorder.RecordAsync(
                context,
                requestSummary.RawMethod ?? "[malformed]",
                ObservationPhases.Malformed,
                durationMs: 0,
                requestSummary.SummaryJson,
                context.Response.StatusCode,
                context.RequestAborted.IsCancellationRequested);
            return;
        }

        context.Request.Body.Position = 0;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var wasCancelled = context.RequestAborted.IsCancellationRequested;

            if (requestSummary.Method is null)
            {
                await observationRecorder.RecordAsync(
                    context,
                    requestSummary.RawMethod ?? "[malformed]",
                    ObservationPhases.Malformed,
                    stopwatch.ElapsedMilliseconds,
                    requestSummary.SummaryJson,
                    statusCode,
                    wasCancelled);
            }
            else
            {
                await observationRecorder.RecordAsync(
                    context,
                    requestSummary.Method,
                    McpObservationPhaseMapper.Map(requestSummary.Method),
                    stopwatch.ElapsedMilliseconds,
                    requestSummary.SummaryJson,
                    statusCode,
                    wasCancelled);
            }
        }
    }

    private static bool IsMcpPost(HttpContext context) =>
        context.Request.Path.StartsWithSegments(McpPaths.McpEndpoint) &&
        HttpMethods.IsPost(context.Request.Method);

    private static async Task<McpJsonRpcRequestSummary> TryReadJsonRpcRequestAsync(HttpContext context)
    {
        try
        {
            var request = await ScenarioWireRequestReader.TryReadPostAsync(
                context.Request,
                context.RequestAborted);
            return ToSummary(request);
        }
        catch (IOException)
        {
            context.Items[ScenarioSessionItemKeys.WireBodyTooLarge] = true;
            return new McpJsonRpcRequestSummary(null, null, null);
        }
    }

    private static McpJsonRpcRequestSummary ToSummary(ScenarioWireJsonRpcRequest? request)
    {
        if (request?.Method is null)
        {
            return new McpJsonRpcRequestSummary(null, null, request?.Method);
        }

        return new McpJsonRpcRequestSummary(
            request.Method,
            BuildParamsSummary(request.Method, request.Params),
            request.Method);
    }

    private static string? BuildParamsSummary(string? method, JsonElement? paramsElement)
    {
        if (paramsElement is null)
        {
            return null;
        }

        var paramsValue = paramsElement.Value;

        if (method == McpMethods.Initialize)
        {
            string? clientName = null;
            string? clientVersion = null;
            string? protocolVersion = null;

            if (paramsValue.TryGetProperty(McpJsonRpcFields.ProtocolVersion, out var protocolVersionElement))
            {
                protocolVersion = protocolVersionElement.GetString();
            }

            if (paramsValue.TryGetProperty(McpJsonRpcFields.ClientInfo, out var clientInfoElement))
            {
                if (clientInfoElement.TryGetProperty(McpJsonRpcFields.Name, out var nameElement))
                {
                    clientName = nameElement.GetString();
                }

                if (clientInfoElement.TryGetProperty(McpJsonRpcFields.Version, out var versionElement))
                {
                    clientVersion = versionElement.GetString();
                }
            }

            return JsonSerializer.Serialize(new
            {
                protocolVersion,
                clientName,
                clientVersion,
            });
        }

        if (method == McpMethods.ToolsCall && paramsValue.TryGetProperty(McpJsonRpcFields.Name, out var toolNameElement))
        {
            return JsonSerializer.Serialize(new { name = toolNameElement.GetString() });
        }

        if (paramsValue.ValueKind is JsonValueKind.Object && paramsValue.GetRawText().Length <= 512)
        {
            return paramsValue.GetRawText();
        }

        return null;
    }

    private sealed record McpJsonRpcRequestSummary(string? Method, string? SummaryJson, string? RawMethod);
}
