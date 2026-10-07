using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using McpTestServer.API.Constants;
using McpTestServer.API.Options;
using McpTestServer.Infrastructure.Options;
using Testcontainers.PostgreSql;

namespace McpTestServer.API.Tests.Fixtures;

public class McpTestServerApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithCleanUp(true)
        .Build();

    protected virtual string EnvironmentName => "Development";

    public const string AdminEmailDomain = "admin.example.com";

    public string McpPat { get; set; } = "test-pat-token";

    public async Task InitializeAsync()
    {
        await DbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await DbContainer.DisposeAsync();
    }

    public HttpClient CreateAuthenticatedClient(string email = "test-user@example.com")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.TestAuthHeaderName, email);
        return client;
    }

    protected virtual Dictionary<string, string?> BuildConfiguration()
    {
        return new Dictionary<string, string?>
        {
            [$"{DatabaseOptions.SectionName}:CONNECTION_STRING"] = DbContainer.GetConnectionString(),
            [$"{McpPatOptions.SectionName}:MCP_PAT"] = McpPat,
            ["INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID"] = "test-client-id",
            ["INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_SECRET"] = "test-client-secret",
            [AdminAccessConstants.EmailDomainEnvKey] = AdminEmailDomain,
        };
    }

    protected virtual bool UseTestAuthHandler => true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(BuildConfiguration());
        });

        if (!UseTestAuthHandler)
        {
            return;
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme,
                _ => { });
        });
    }
}

public sealed class McpTestServerProductionApiFixture : McpTestServerApiFixture
{
    protected override string EnvironmentName => "Production";

    protected override Dictionary<string, string?> BuildConfiguration()
    {
        var config = base.BuildConfiguration();
        config[OAuthPublicOriginOptions.EnvKey] = "https://mcp-test.example.com";
        config[$"{McpPatOptions.SectionName}:MCP_PAT"] = "production-test-pat";
        return config;
    }
}

public sealed class McpTestServerApiNoGoogleAuthFixture : McpTestServerApiFixture
{
    protected override Dictionary<string, string?> BuildConfiguration()
    {
        var config = base.BuildConfiguration();
        config["INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_ID"] = string.Empty;
        config["INTERNAL_AUTHENTICATION_GOOGLE_CLIENT_SECRET"] = string.Empty;
        return config;
    }
}

[CollectionDefinition(nameof(McpTestServerApiCollection))]
public sealed class McpTestServerApiCollection : ICollectionFixture<McpTestServerApiFixture>
{
}

[CollectionDefinition(nameof(McpTestServerProductionApiCollection))]
public sealed class McpTestServerProductionApiCollection : ICollectionFixture<McpTestServerProductionApiFixture>
{
}
