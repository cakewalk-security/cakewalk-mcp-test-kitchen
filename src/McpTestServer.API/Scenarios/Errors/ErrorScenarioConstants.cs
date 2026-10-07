namespace McpTestServer.API.Scenarios.Errors;

public static class ConnectionCloseTargets
{
    public const string ToolCall = "tool_call";

    public const string Initialize = "initialize";
}

public static class MalformedPayloadKinds
{
    public const string InvalidJson = "invalid_json";

    public const string WrongId = "wrong_id";

    public const string MissingJsonRpc = "missing_jsonrpc";

    public const string DuplicateResponse = "duplicate_response";
}

public static class MalformedPayloadTargets
{
    public const string ToolCall = "tool_call";

    public const string Initialize = "initialize";

    public const string HttpRequest = "http_request";
}
