using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class CatalogFacetUnavailableParams
{
    [JsonPropertyName("unavailableFacets")]
    public string[] UnavailableFacets { get; init; } = [CatalogFacetNames.Resources];
}
