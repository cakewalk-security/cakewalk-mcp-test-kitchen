using FluentAssertions;
using McpTestServer.API.Constants;
using McpTestServer.API.Mcp;
using Xunit;

namespace McpTestServer.API.Tests.Mcp;

public sealed class McpObservationCatalogTests
{
    [Theory]
    [InlineData(McpMethods.ResourcesList, false)]
    [InlineData(McpMethods.ResourcesRead, false)]
    [InlineData(McpMethods.PromptsList, false)]
    [InlineData(McpMethods.PromptsGet, false)]
    [InlineData(McpMethods.ToolsList, true)]
    [InlineData(McpMethods.ToolsCall, true)]
    [InlineData(McpMethods.Initialize, true)]
    public void ShouldAttachScenarioId_only_for_non_catalog_facets(string method, bool expected) =>
        McpObservationCatalog.ShouldAttachScenarioId(method).Should().Be(expected);

    [Theory]
    [InlineData(McpMethods.ToolsCall, McpObservationCatalog.CategoryTools, true)]
    [InlineData(McpMethods.ResourcesRead, McpObservationCatalog.CategoryResources, true)]
    [InlineData(McpMethods.PromptsGet, McpObservationCatalog.CategoryPrompts, true)]
    [InlineData(McpMethods.ResourcesRead, McpObservationCatalog.CategoryTools, false)]
    [InlineData(McpMethods.Initialize, McpObservationCatalog.CategoryAll, true)]
    public void MatchesCategory_filters_by_method_prefix(string method, string category, bool expected) =>
        McpObservationCatalog.MatchesCategory(method, category).Should().Be(expected);
}
