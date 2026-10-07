using System.ComponentModel;
using System.Text.Json;
using McpTestServer.API.Constants;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Baseline;

internal sealed class BaselineCatalogPrimitives
{
    public const string ValidResourceText =
        "Valid resource content from MCP test server. Use valid_resource / test://resources/valid to exercise happy-path resources/read.";

    [McpServerResource(
        Name = McpResourceNames.ValidResource,
        UriTemplate = McpResourceUris.Valid,
        MimeType = "text/plain"),
     Description("Always returns valid text resource content for MCP client testing.")]
    public string ValidResource() => ValidResourceText;

    [McpServerResource(
        Name = McpResourceNames.InvalidResource,
        UriTemplate = McpResourceUris.Invalid,
        MimeType = "text/plain"),
     Description(
         "Advertised for testing invalid resource payloads. resources/read returns a schema-invalid JSON-RPC result " +
         "(intercepted at the wire layer).")]
    public string InvalidResourcePlaceholder() =>
        throw new InvalidOperationException(
            $"{McpResourceNames.InvalidResource} reads are handled by {nameof(BaselineCatalogWireInterceptor)}.");

    [McpServerPrompt(Name = McpPromptNames.ValidPrompt),
     Description("Always returns a valid user text prompt message for MCP client testing.")]
    public string ValidPrompt(
        [Description("Optional topic label included in the prompt text.")]
        string? topic = null)
    {
        var label = string.IsNullOrWhiteSpace(topic) ? "general testing" : topic;
        return $"You are testing the MCP test server. Topic: {label}";
    }

    [McpServerPrompt(Name = McpPromptNames.InvalidPrompt),
     Description(
         "Advertised for testing invalid prompt payloads. prompts/get returns a schema-invalid JSON-RPC result " +
         "(intercepted at the wire layer).")]
    public string InvalidPromptPlaceholder() =>
        throw new InvalidOperationException(
            $"{McpPromptNames.InvalidPrompt} reads are handled by {nameof(BaselineCatalogWireInterceptor)}.");
}

internal static class BaselineCatalogWireInterceptor
{
    public static async ValueTask<WireDecision> TryInterceptAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(httpContext.Request, cancellationToken);
        if (request?.Method is null)
        {
            return WireDecision.Continue;
        }

        return request.Method switch
        {
            McpMethods.ResourcesRead when TryGetResourceUri(request.Params, out var uri)
                && string.Equals(uri, McpResourceUris.Invalid, StringComparison.Ordinal) =>
                WireDecision.RespondWithBody(
                    StatusCodes.Status200OK,
                    ScenarioWireJsonRpc.Result(request.Id, InvalidResourceResult())),

            McpMethods.PromptsGet when TryGetPromptName(request.Params, out var name)
                && string.Equals(name, McpPromptNames.InvalidPrompt, StringComparison.Ordinal) =>
                WireDecision.RespondWithBody(
                    StatusCodes.Status200OK,
                    ScenarioWireJsonRpc.Result(request.Id, InvalidPromptResult())),

            _ => WireDecision.Continue,
        };
    }

    private static bool TryGetResourceUri(JsonElement? parameters, out string uri)
    {
        uri = string.Empty;
        if (parameters?.ValueKind != JsonValueKind.Object
            || !parameters.Value.TryGetProperty("uri", out var uriElement)
            || uriElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        uri = uriElement.GetString() ?? string.Empty;
        return !string.IsNullOrEmpty(uri);
    }

    private static bool TryGetPromptName(JsonElement? parameters, out string name)
    {
        name = string.Empty;
        if (parameters?.ValueKind != JsonValueKind.Object
            || !parameters.Value.TryGetProperty("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        name = nameElement.GetString() ?? string.Empty;
        return !string.IsNullOrEmpty(name);
    }

    private static object InvalidResourceResult() =>
        new
        {
            contents = new object[]
            {
                new
                {
                    type = "blob",
                    blob = "not-valid-base64!!!",
                    mimeType = "not-a-mime-type",
                },
            },
        };

    private static object InvalidPromptResult() =>
        new
        {
            messages = new object[]
            {
                new
                {
                    content = new
                    {
                        type = "unknown_type",
                        text = "missing role field",
                    },
                },
            },
        };
}
