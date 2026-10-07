using System.Security.Claims;
using McpTestServer.API.Constants;
using McpTestServer.API.Options;

namespace McpTestServer.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserEmail(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value
        ?? user.FindFirst("email")?.Value;

    public static string? GetAuthProvider(this ClaimsPrincipal user) =>
        user.FindFirst(AuthProviderClaims.Type)?.Value;

    public static bool HasVerifiedGoogleEmail(this ClaimsPrincipal user) =>
        string.Equals(user.FindFirst(GoogleOidcClaims.EmailVerified)?.Value, "true", StringComparison.OrdinalIgnoreCase);

    // The email suffix alone is not proof of domain ownership; Google's hosted-domain claim is.
    public static bool CanAccessAdmin(this ClaimsPrincipal user, AdminAccessOptions adminAccessOptions) =>
        adminAccessOptions.IsAdminEmail(user.GetUserEmail())
            && adminAccessOptions.IsAdminDomain(user.FindFirst(GoogleOidcClaims.HostedDomain)?.Value)
            && string.Equals(user.GetAuthProvider(), AuthProviderClaims.Google, StringComparison.OrdinalIgnoreCase);
}
