using System.Text.Json;
using McpTestServer.API.Constants;

namespace McpTestServer.API.Scenarios;

public sealed record ScenarioWireJsonRpcRequest(object? Id, string? Method, JsonElement? Params)
{
    public string? Cursor =>
        Params?.ValueKind == JsonValueKind.Object
        && Params.Value.TryGetProperty("cursor", out var cursorElement)
        && cursorElement.ValueKind == JsonValueKind.String
            ? cursorElement.GetString()
            : null;
}

public static class ScenarioWireRequestReader
{
    private static readonly object ParseFailedSentinel = new();

    public static async ValueTask<ScenarioWireJsonRpcRequest?> TryReadPostAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return null;
        }

        if (request.HttpContext.Items.TryGetValue(ScenarioSessionItemKeys.ParsedWireRequest, out var cached))
        {
            return cached as ScenarioWireJsonRpcRequest;
        }

        request.EnableBuffering(
            bufferThreshold: McpObservationConstants.RequestBodyBufferLimitBytes,
            bufferLimit: McpObservationConstants.RequestBodyBufferLimitBytes);

        request.Body.Position = 0;
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
            request.Body.Position = 0;

            var root = document.RootElement;
            object? id = null;
            if (root.TryGetProperty(McpJsonRpcFields.Id, out var idElement))
            {
                id = idElement.ValueKind switch
                {
                    JsonValueKind.Number when idElement.TryGetInt32(out var intId) => intId,
                    JsonValueKind.String => idElement.GetString(),
                    _ => null,
                };
            }

            var method = root.TryGetProperty(McpJsonRpcFields.Method, out var methodElement)
                ? methodElement.GetString()
                : null;

            JsonElement? parameters = root.TryGetProperty(McpJsonRpcFields.Params, out var paramsElement)
                ? paramsElement.Clone()
                : null;

            var parsed = new ScenarioWireJsonRpcRequest(id, method, parameters);
            request.HttpContext.Items[ScenarioSessionItemKeys.ParsedWireRequest] = parsed;
            return parsed;
        }
        catch (JsonException)
        {
            request.Body.Position = 0;
            request.HttpContext.Items[ScenarioSessionItemKeys.ParsedWireRequest] = ParseFailedSentinel;
            return null;
        }
        catch (IOException)
        {
            request.HttpContext.Items[ScenarioSessionItemKeys.WireBodyTooLarge] = true;
            request.HttpContext.Items[ScenarioSessionItemKeys.ParsedWireRequest] = ParseFailedSentinel;
            return null;
        }
    }

    public static bool IsWireBodyTooLarge(HttpContext context) =>
        context.Items.ContainsKey(ScenarioSessionItemKeys.WireBodyTooLarge);
}
