using System.Security.Claims;
using McpTestServer.API.Constants;

namespace McpTestServer.API.Extensions;

internal static class AuthTicketExtensions
{
    public static void AddAuthProviderClaim(ClaimsPrincipal principal, string provider)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var existing = identity.FindFirst(AuthProviderClaims.Type);
        if (existing is not null)
        {
            identity.RemoveClaim(existing);
        }

        identity.AddClaim(new Claim(AuthProviderClaims.Type, provider));
    }
}
