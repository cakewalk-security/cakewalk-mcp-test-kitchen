using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace McpTestServer.API.Options;

public sealed class LegacyHostRedirectOptions
{
    public const string CanonicalOriginEnvKey = "MCP_TEST_SERVER_CANONICAL_ORIGIN";

    public const string LegacyHostsEnvKey = "MCP_TEST_SERVER_LEGACY_HOSTS";

    [Required]
    public string MCP_TEST_SERVER_CANONICAL_ORIGIN { get; set; } = string.Empty;

    [Required]
    public string MCP_TEST_SERVER_LEGACY_HOSTS { get; set; } = string.Empty;

    private HashSet<string>? legacyHosts;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(MCP_TEST_SERVER_CANONICAL_ORIGIN)
        && LegacyHostSet.Count > 0;

    public IReadOnlySet<string> LegacyHostSet =>
        legacyHosts ??= ParseHosts(MCP_TEST_SERVER_LEGACY_HOSTS);

    public string BuildRedirectUrl(HttpRequest request)
    {
        var origin = MCP_TEST_SERVER_CANONICAL_ORIGIN.TrimEnd('/');
        var path = request.Path.HasValue ? request.Path.Value! : "/";
        var query = request.QueryString.HasValue ? request.QueryString.Value! : string.Empty;
        return $"{origin}{path}{query}";
    }

    private static HashSet<string> ParseHosts(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
