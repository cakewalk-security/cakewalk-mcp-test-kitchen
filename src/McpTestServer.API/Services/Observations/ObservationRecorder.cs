using System.Text.Json;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Mcp;
using McpTestServer.API.Scenarios;
using McpTestServer.Infrastructure;
using McpTestServer.Infrastructure.Constants;
using McpTestServer.Infrastructure.Entities;
using Microsoft.AspNetCore.Http;

namespace McpTestServer.API.Services.Observations;

public interface IObservationRecorder
{
    Task RecordAsync(
        HttpContext httpContext,
        string method,
        string phase,
        long durationMs,
        string? requestParamsJson,
        int statusCode,
        bool wasCancelled,
        string? scenarioIdOverride = null);
}

public sealed class ObservationRecorder(
    McpTestServerContext db,
    IObservationStreamHub observationStreamHub) : IObservationRecorder
{
    public async Task RecordAsync(
        HttpContext httpContext,
        string method,
        string phase,
        long durationMs,
        string? requestParamsJson,
        int statusCode,
        bool wasCancelled,
        string? scenarioIdOverride = null)
    {
        var headersJson = HeaderRedaction.SerializeRedactedHeaders(httpContext.Request.Headers);
        var scenarioId = scenarioIdOverride ?? ResolveScenarioId(httpContext, method);
        var clientProtocolVersion = ClientProtocolVersionResolver.Resolve(
            httpContext.Request.Headers,
            method,
            requestParamsJson);

        var observation = new Observation
        {
            RequestId = httpContext.TraceIdentifier,
            CallerEmail = httpContext.Items[McpCallerConstants.CallerEmailItemKey] as string,
            ScenarioId = scenarioId,
            Method = method,
            Phase = phase,
            DurationMs = durationMs,
            StatusCode = statusCode,
            WasCancelled = wasCancelled,
            HeadersJson = headersJson,
            RequestParamsJson = requestParamsJson,
            ClientProtocolVersion = clientProtocolVersion,
            OccurredAt = DateTime.UtcNow,
        };

        db.Observations.Add(observation);
        await db.SaveChangesAsync(CancellationToken.None);
        observationStreamHub.Publish(ObservationResponse.FromEntity(observation));
    }

    private static string? ResolveScenarioId(HttpContext httpContext, string method)
    {
        if (!McpObservationCatalog.ShouldAttachScenarioId(method))
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(ScenarioSessionItemKeys.ScenarioSession, out var itemSession)
            && itemSession is ScenarioSession configuredSession)
        {
            return configuredSession.ScenarioId;
        }

        var registry = httpContext.RequestServices.GetService<IScenarioSessionRegistry>();
        var sessionId = httpContext.Request.Headers.TryGetValue(AuthHeaderNames.McpSessionId, out var sessionValues)
            ? sessionValues.FirstOrDefault()
            : null;

        var callerEmail = httpContext.Items[McpCallerConstants.CallerEmailItemKey] as string;
        if (registry?.TryGet(sessionId, out var session) == true && session is not null && session.BelongsTo(callerEmail))
        {
            return session.ScenarioId;
        }

        return null;
    }
}

internal static class HeaderRedaction
{
    private static readonly HashSet<string> AllowedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "accept",
        "content-type",
        "content-length",
        "host",
        "user-agent",
        "mcp-protocol-version",
    };

    public static string SerializeRedactedHeaders(IHeaderDictionary headers)
    {
        var redacted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            redacted[header.Key] = AllowedHeaders.Contains(header.Key)
                ? header.Value.ToString()
                : "[redacted]";
        }

        return JsonSerializer.Serialize(redacted);
    }
}
