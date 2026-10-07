using System.Text.Json;
using McpTestServer.API.Constants;
using McpTestServer.API.Mcp;

namespace McpTestServer.API.Services.Observations;

internal static class ClientProtocolVersionResolver
{
    private const string RequestParamsProtocolVersionKey = "protocolVersion";

    public static string? Resolve(IHeaderDictionary headers, string? method, string? requestParamsJson)
    {
        if (headers.TryGetValue(McpProtocolHeaders.ProtocolVersion, out var headerValues))
        {
            var headerValue = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue;
            }
        }

        if (method == McpMethods.Initialize)
        {
            return TryReadProtocolVersionFromRequestParams(requestParamsJson);
        }

        return null;
    }

    private static string? TryReadProtocolVersionFromRequestParams(string? requestParamsJson)
    {
        if (string.IsNullOrWhiteSpace(requestParamsJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(requestParamsJson);
            if (document.RootElement.TryGetProperty(RequestParamsProtocolVersionKey, out var protocolVersionElement))
            {
                return protocolVersionElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Ignore malformed summary JSON.
        }

        return null;
    }
}
