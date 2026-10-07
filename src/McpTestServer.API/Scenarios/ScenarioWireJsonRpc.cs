using System.Text.Json;

namespace McpTestServer.API.Scenarios;

public static class ScenarioWireJsonRpc
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string FormatId(object? id) => JsonSerializer.Serialize(id, SerializerOptions);

    public static string Error(object? id, int code, string message) =>
        JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id,
                error = new { code, message },
            },
            SerializerOptions);

    public static string Result(object? id, object result) =>
        JsonSerializer.Serialize(
            new
            {
                jsonrpc = "2.0",
                id,
                result,
            },
            SerializerOptions);
}
