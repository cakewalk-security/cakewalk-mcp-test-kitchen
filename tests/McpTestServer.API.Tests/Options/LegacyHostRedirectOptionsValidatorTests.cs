using FluentAssertions;
using McpTestServer.API.Options;
using McpTestServer.API.Options.Validation;
using Xunit;

namespace McpTestServer.API.Tests.Options;

public sealed class LegacyHostRedirectOptionsValidatorTests
{
    private readonly LegacyHostRedirectOptionsValidator validator = new();

    [Fact]
    public void Validate_succeeds_when_redirect_options_are_not_configured()
    {
        var result = validator.Validate(null, new LegacyHostRedirectOptions());
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_when_only_canonical_origin_is_configured()
    {
        var result = validator.Validate(
            null,
            new LegacyHostRedirectOptions
            {
                MCP_TEST_SERVER_CANONICAL_ORIGIN = "https://mcp-test-kitchen.cakewalk.security",
            });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("must both be set");
    }

    [Fact]
    public void Validate_fails_when_canonical_origin_includes_a_path()
    {
        var result = validator.Validate(
            null,
            new LegacyHostRedirectOptions
            {
                MCP_TEST_SERVER_CANONICAL_ORIGIN = "https://mcp-test-kitchen.cakewalk.security/console",
                MCP_TEST_SERVER_LEGACY_HOSTS = "mcp-test-server-cakewalk.fly.dev",
            });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("without a path");
    }

    [Fact]
    public void Validate_succeeds_for_valid_redirect_configuration()
    {
        var result = validator.Validate(
            null,
            new LegacyHostRedirectOptions
            {
                MCP_TEST_SERVER_CANONICAL_ORIGIN = "https://mcp-test-kitchen.cakewalk.security",
                MCP_TEST_SERVER_LEGACY_HOSTS = "mcp-test-server-cakewalk.fly.dev, legacy.example.com",
            });

        result.Succeeded.Should().BeTrue();
    }
}
