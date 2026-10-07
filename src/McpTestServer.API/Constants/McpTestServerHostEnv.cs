namespace McpTestServer.API.Constants;

public static class McpTestServerHostEnv
{
    public const string OAuthPublicOrigin = "MCP_TEST_SERVER_OAUTH_PUBLIC_ORIGIN";

    public const string DataProtectionKeysPath = "MCP_TEST_SERVER_DATA_PROTECTION_KEYS_PATH";

    // Set to "true" only when the app is reachable exclusively through a reverse proxy that
    // appends the client IP to X-Forwarded-For (e.g. Fly.io); otherwise clients can spoof it.
    public const string TrustProxyForwardedFor = "MCP_TEST_SERVER_TRUST_PROXY_FORWARDED_FOR";
}

public static class McpTestServerDataProtectionConstants
{
    public const string ApplicationName = "mcp-test-server";
}
