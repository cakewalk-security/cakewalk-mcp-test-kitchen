using FluentAssertions;
using McpTestServer.Infrastructure.Options;
using Xunit;

namespace McpTestServer.API.Tests.Options;

public sealed class DatabaseConnectionStringResolverTests
{
    [Fact]
    public void Resolve_prefers_database_url_over_section_value()
    {
        var resolved = DatabaseConnectionStringResolver.Resolve(
            "postgres://user:secret@db.example.com:5432/mcp_test_server",
            "Host=localhost;Port=15433;Database=ignored");

        resolved.Should().Contain("Host=db.example.com");
        resolved.Should().Contain("Database=mcp_test_server");
        resolved.Should().Contain("Username=user");
    }

    [Fact]
    public void Resolve_parses_sslmode_disable()
    {
        var resolved = DatabaseConnectionStringResolver.Resolve(
            "postgres://user:secret@db.example.com:5432/mcp_test_server?sslmode=disable",
            null);

        resolved.Should().Contain("Host=db.example.com");
        resolved.Should().Contain("Database=mcp_test_server");
        resolved.Should().Contain("Username=user");
        resolved.Should().Contain("SSL Mode=Disable");
    }

    [Fact]
    public void Resolve_parses_sslmode_verify_ca_and_verify_full()
    {
        var verifyCa = DatabaseConnectionStringResolver.Resolve(
            "postgres://user:secret@db.example.com:5432/mcp?sslmode=verify-ca",
            null);
        verifyCa.Should().Contain("SSL Mode=VerifyCA");

        var verifyFull = DatabaseConnectionStringResolver.Resolve(
            "postgres://user:secret@db.example.com:5432/mcp?sslmode=verify-full",
            null);
        verifyFull.Should().Contain("SSL Mode=VerifyFull");
    }

    [Fact]
    public void Resolve_falls_back_to_section_connection_string()
    {
        const string sectionValue = "Host=localhost;Port=15433;Database=mcp_test_server";
        var resolved = DatabaseConnectionStringResolver.Resolve(null, sectionValue);
        resolved.Should().Be(sectionValue);
    }
}
