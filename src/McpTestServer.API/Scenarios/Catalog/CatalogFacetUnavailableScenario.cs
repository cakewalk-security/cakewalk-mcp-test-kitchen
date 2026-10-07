using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;
using ModelContextProtocol;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class CatalogFacetUnavailableScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.CatalogFacetUnavailable,
        "Catalog facet unavailable",
        "Returns JSON-RPC MethodNotFound for resources/list or prompts/list to simulate unsupported catalog facets.",
        ScenarioAreas.Catalog,
        """{"unavailableFacets":["resources"]}""");

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);
        if (request?.Method is null)
        {
            return WireDecision.Continue;
        }

        var parameters = context.Session.GetParams<CatalogFacetUnavailableParams>();
        var unavailable = new HashSet<string>(parameters.UnavailableFacets ?? [], StringComparer.Ordinal);

        var isUnavailable = request.Method switch
        {
            McpMethods.ResourcesList => unavailable.Contains(CatalogFacetNames.Resources),
            McpMethods.PromptsList => unavailable.Contains(CatalogFacetNames.Prompts),
            _ => false,
        };

        if (!isUnavailable)
        {
            return WireDecision.Continue;
        }

        return WireDecision.RespondWithBody(
            StatusCodes.Status200OK,
            ScenarioWireJsonRpc.Error(request.Id, (int)McpErrorCode.MethodNotFound, "Method not found"));
    }
}
