namespace McpTestServer.API.Constants;

public static class AuthProviderClaims
{
    public const string Type = "auth_provider";

    public const string Google = "google";

    public const string GitHub = "github";
}

public static class GoogleOidcClaims
{
    public const string EmailVerified = "email_verified";

    // Google Workspace hosted domain; absent for consumer accounts.
    public const string HostedDomain = "hd";

    public static readonly string[] ValidIssuers = ["https://accounts.google.com", "accounts.google.com"];
}
