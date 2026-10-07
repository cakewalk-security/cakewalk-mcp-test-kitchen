using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class CatalogPaginationParams
{
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; init; } = 10;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; } = 3;

    [JsonPropertyName("facet")]
    public string Facet { get; init; } = CatalogFacetNames.Resources;

    [JsonPropertyName("fault")]
    public string Fault { get; init; } = CatalogPaginationFaults.None;

    [JsonPropertyName("faultAtPage")]
    public int FaultAtPage { get; init; } = 2;
}
