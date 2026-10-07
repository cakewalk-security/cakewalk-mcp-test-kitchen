namespace McpTestServer.API.Constants;

/// <summary>
/// Latest MCP protocol revision served in stateless mode (native MRTR for elicitation).
/// Legacy 2025-11-25 clients may still connect via the initialize back-compat path.
/// </summary>
public static class McpProtocolConstants
{
    public const string LatestSupportedProtocolVersion = "2026-07-28";
}

public static class McpProtocolHeaders
{
    public const string ProtocolVersion = "MCP-Protocol-Version";

    public const string Method = "Mcp-Method";

    public const string Name = "Mcp-Name";

    public const string ParamPrefix = "Mcp-Param-";
}

public static class McpTransportConstants
{
    /// <summary>
    /// Streamable HTTP runs without MCP sessions. Keep in lockstep with
    /// <c>HttpServerTransportOptions.Stateless</c> in DI.
    /// </summary>
    public const bool Stateless = true;

    public const string StatelessLabel = "Stateless = true";
}

public static class McpCsharpSdkV2Article
{
    public const string Url = "https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/";

    public const string Title = "Announcing v2.0 of the official MCP C# SDK";
}
