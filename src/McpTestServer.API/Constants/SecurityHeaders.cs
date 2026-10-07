namespace McpTestServer.API.Constants;

public static class SecurityHeaders
{
    public const string StrictTransportSecurity = "max-age=31536000";

    public const string ContentTypeOptions = "nosniff";

    public const string FrameOptions = "DENY";

    public const string ContentSecurityPolicy = "frame-ancestors 'none'";

    public const string ReferrerPolicy = "strict-origin-when-cross-origin";
}
