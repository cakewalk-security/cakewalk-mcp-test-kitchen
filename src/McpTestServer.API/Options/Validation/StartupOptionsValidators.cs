using McpTestServer.API.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Options.Validation;

public sealed class OAuthPublicOriginOptionsValidator(IHostEnvironment environment) : IValidateOptions<OAuthPublicOriginOptions>
{
    public ValidateOptionsResult Validate(string? name, OAuthPublicOriginOptions options)
    {
        if (environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.MCP_TEST_SERVER_OAUTH_PUBLIC_ORIGIN))
        {
            return ValidateOptionsResult.Fail("MCP_TEST_SERVER_OAUTH_PUBLIC_ORIGIN is required outside Development.");
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class UnauthenticatedRedirectOptionsValidator : IValidateOptions<UnauthenticatedRedirectOptions>
{
    public ValidateOptionsResult Validate(string? name, UnauthenticatedRedirectOptions options)
    {
        var url = string.IsNullOrWhiteSpace(options.MCP_TEST_SERVER_UNAUTHENTICATED_REDIRECT_URL)
            ? UnauthenticatedRedirectOptions.DefaultUrl
            : options.MCP_TEST_SERVER_UNAUTHENTICATED_REDIRECT_URL;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail(
                "MCP_TEST_SERVER_UNAUTHENTICATED_REDIRECT_URL must be an absolute http or https URL.");
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class LegacyHostRedirectOptionsValidator : IValidateOptions<LegacyHostRedirectOptions>
{
    public ValidateOptionsResult Validate(string? name, LegacyHostRedirectOptions options)
    {
        var canonicalOrigin = options.MCP_TEST_SERVER_CANONICAL_ORIGIN?.Trim();
        var legacyHosts = options.MCP_TEST_SERVER_LEGACY_HOSTS?.Trim();

        if (string.IsNullOrWhiteSpace(canonicalOrigin) && string.IsNullOrWhiteSpace(legacyHosts))
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(canonicalOrigin) || string.IsNullOrWhiteSpace(legacyHosts))
        {
            return ValidateOptionsResult.Fail(
                "MCP_TEST_SERVER_CANONICAL_ORIGIN and MCP_TEST_SERVER_LEGACY_HOSTS must both be set to enable legacy host redirects.");
        }

        if (!Uri.TryCreate(canonicalOrigin, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.PathAndQuery.Trim('/'))
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return ValidateOptionsResult.Fail(
                "MCP_TEST_SERVER_CANONICAL_ORIGIN must be an absolute https origin without a path.");
        }

        foreach (var host in options.LegacyHostSet)
        {
            if (host.Contains('/', StringComparison.Ordinal) || host.Contains(':', StringComparison.Ordinal))
            {
                return ValidateOptionsResult.Fail(
                    "MCP_TEST_SERVER_LEGACY_HOSTS must contain hostnames only, separated by commas.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class McpPatOptionsValidator(IHostEnvironment environment) : IValidateOptions<McpPatOptions>
{
    public ValidateOptionsResult Validate(string? name, McpPatOptions options)
    {
        if (environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.MCP_PAT))
        {
            return ValidateOptionsResult.Fail("MCP_PAT must be configured outside Development.");
        }

        return ValidateOptionsResult.Success;
    }
}
