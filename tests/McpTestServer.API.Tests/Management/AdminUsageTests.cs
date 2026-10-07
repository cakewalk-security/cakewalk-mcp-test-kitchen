using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Controllers.Management.Models;
using McpTestServer.API.Tests.Fixtures;
using Xunit;

namespace McpTestServer.API.Tests.Management;

[Collection(nameof(McpTestServerApiCollection))]
public sealed class AdminUsageTests
{
    private readonly McpTestServerApiFixture _fixture;

    public AdminUsageTests(McpTestServerApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Usage_returns_404_for_non_admin_users()
    {
        using var client = _fixture.CreateAuthenticatedClient("bob@example.com");

        var response = await client.GetAsync("/api/management/admin/usage");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Usage_returns_404_for_unauthenticated_requests()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/management/admin/usage");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Usage_returns_masked_emails_and_counts_for_admin_users()
    {
        using var aliceClient = _fixture.CreateAuthenticatedClient("alice@example.com");
        var alicePat = await aliceClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var bobClient = _fixture.CreateAuthenticatedClient("bob@other.org");
        var bobPat = await bobClient.GetFromJsonAsync<UserMcpPatResponse>("/api/management/me/pat");

        using var mcpAliceClient = _fixture.CreateClient();
        var mcpAlice = new McpSessionClient(mcpAliceClient, alicePat!.Pat);
        await mcpAlice.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        using var mcpBobClient = _fixture.CreateClient();
        var mcpBob = new McpSessionClient(mcpBobClient, bobPat!.Pat);
        await mcpBob.PostStatelessAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = McpMethods.ToolsList,
            @params = new { },
        });

        using var adminClient = _fixture.CreateAuthenticatedClient($"admin@{McpTestServerApiFixture.AdminEmailDomain}");
        var me = await adminClient.GetFromJsonAsync<CurrentUserResponse>("/api/management/me");
        me!.CanAccessAdmin.Should().BeTrue();

        var usage = await adminClient.GetFromJsonAsync<AdminUsageResponse>("/api/management/admin/usage");
        usage.Should().NotBeNull();
        usage!.Totals.RegisteredUserCount.Should().BeGreaterThanOrEqualTo(2);
        usage.Totals.ObservationsLast24Hours.Should().BeGreaterThanOrEqualTo(2);
        usage.Users.Should().Contain(user =>
            user.MaskedEmail == "***@example.com"
            && user.ObservationCountLast24Hours >= 1);
        usage.Users.Should().Contain(user =>
            user.MaskedEmail == "***@other.org"
            && user.ObservationCountLast24Hours >= 1);
        usage.Users.Should().OnlyContain(user => user.MaskedEmail.StartsWith("***@"));
        usage.Users.Should().NotContain(user => user.MaskedEmail.Contains("alice@"));
        usage.Users.Should().NotContain(user => user.MaskedEmail.Contains("bob@"));
    }

    [Fact]
    public async Task Me_omits_can_access_admin_for_github_admin_domain_users()
    {
        using var googleClient = _fixture.CreateAuthenticatedClient($"ops@{McpTestServerApiFixture.AdminEmailDomain}");
        using var githubClient = _fixture.CreateAuthenticatedClient($"ops@{McpTestServerApiFixture.AdminEmailDomain}");
        githubClient.DefaultRequestHeaders.Add(TestAuthHandler.TestAuthProviderHeaderName, AuthProviderClaims.GitHub);

        var googleBody = await googleClient.GetStringAsync("/api/management/me");
        var githubBody = await githubClient.GetStringAsync("/api/management/me");

        googleBody.Should().Contain("\"canAccessAdmin\":true");
        githubBody.Should().NotContain("canAccessAdmin");
    }

    [Fact]
    public async Task Me_omits_can_access_admin_for_non_admin_domain()
    {
        using var adminClient = _fixture.CreateAuthenticatedClient($"ops@{McpTestServerApiFixture.AdminEmailDomain}");
        using var externalClient = _fixture.CreateAuthenticatedClient("user@example.com");

        var adminBody = await adminClient.GetStringAsync("/api/management/me");
        var externalBody = await externalClient.GetStringAsync("/api/management/me");

        adminBody.Should().Contain("\"canAccessAdmin\":true");
        externalBody.Should().NotContain("canAccessAdmin");
    }
}
