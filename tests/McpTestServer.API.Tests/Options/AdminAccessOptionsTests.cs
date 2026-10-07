using System.Security.Claims;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Extensions;
using McpTestServer.API.Options;
using Xunit;

namespace McpTestServer.API.Tests.Options;

public sealed class AdminAccessOptionsTests
{
    [Theory]
    [InlineData("example.com")]
    [InlineData("@example.com")]
    [InlineData(" Example.COM ")]
    public void Matches_emails_on_the_configured_domain(string domain)
    {
        var options = new AdminAccessOptions { MCP_TEST_SERVER_ADMIN_EMAIL_DOMAIN = domain };

        options.IsAdminEmail("ops@example.com").Should().BeTrue();
    }

    [Theory]
    [InlineData("ops@other.org")]
    [InlineData("ops@sub.example.com")]
    [InlineData("ops@notexample.com")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_emails_outside_the_configured_domain(string? email)
    {
        var options = new AdminAccessOptions { MCP_TEST_SERVER_ADMIN_EMAIL_DOMAIN = "example.com" };

        options.IsAdminEmail(email).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("@")]
    public void Grants_nobody_admin_when_no_domain_is_configured(string domain)
    {
        var options = new AdminAccessOptions { MCP_TEST_SERVER_ADMIN_EMAIL_DOMAIN = domain };

        options.IsAdminEmail("ops@example.com").Should().BeFalse();
    }

    [Theory]
    [InlineData("example.com", true)]
    [InlineData("EXAMPLE.com", true)]
    [InlineData(null, false)]
    [InlineData("other.org", false)]
    public void Admin_requires_matching_google_hosted_domain(string? hostedDomain, bool expected)
    {
        var options = new AdminAccessOptions { MCP_TEST_SERVER_ADMIN_EMAIL_DOMAIN = "example.com" };
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "ops@example.com"),
            new(AuthProviderClaims.Type, AuthProviderClaims.Google),
        };
        if (hostedDomain is not null)
        {
            claims.Add(new Claim(GoogleOidcClaims.HostedDomain, hostedDomain));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        user.CanAccessAdmin(options).Should().Be(expected);
    }
}
