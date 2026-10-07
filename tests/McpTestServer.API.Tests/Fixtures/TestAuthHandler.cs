using System.Security.Claims;
using System.Text.Encodings.Web;
using McpTestServer.API.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Tests.Fixtures;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Test";
    public const string TestAuthHeaderName = "X-Test-Auth";
    public const string TestAuthProviderHeaderName = "X-Test-Auth-Provider";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestAuthHeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var email = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var provider = Request.Headers.TryGetValue(TestAuthProviderHeaderName, out var providerValues)
            ? providerValues.FirstOrDefault()
            : null;
        if (string.IsNullOrWhiteSpace(provider))
        {
            provider = AuthProviderClaims.Google;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(AuthProviderClaims.Type, provider),
        };

        // Mirror Google Workspace sign-in, which carries the hosted domain claim.
        var atIndex = email.IndexOf('@');
        if (provider == AuthProviderClaims.Google && atIndex >= 0)
        {
            claims.Add(new Claim(GoogleOidcClaims.HostedDomain, email[(atIndex + 1)..]));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
