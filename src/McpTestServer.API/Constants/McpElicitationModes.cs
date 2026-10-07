namespace McpTestServer.API.Constants;

/// <summary>
/// Wire values for <c>ElicitRequestParams.Mode</c>. Consumers such as the Cakewalk MCP Gateway
/// match on this value to decide whether the downstream client advertised the requested mode, and
/// refuse to relay the prompt when it is absent or unrecognised.
/// </summary>
public static class McpElicitationModes
{
    public const string Form = "form";

    public const string Url = "url";
}
