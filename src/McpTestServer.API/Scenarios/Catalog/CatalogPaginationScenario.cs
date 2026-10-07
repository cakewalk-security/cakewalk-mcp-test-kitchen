using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class CatalogPaginationScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.CatalogPagination,
        "Catalog pagination",
        "Returns paginated resources/list or prompts/list responses with configurable edge cases (cursor loop, duplicate page, error after page N).",
        ScenarioAreas.Catalog,
        """
        {
          "totalItems": 10,
          "pageSize": 3,
          "facet": "resources",
          "fault": "none",
          "faultAtPage": 2
        }
        """);

    public override async ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context)
    {
        var request = await ScenarioWireRequestReader.TryReadPostAsync(
            context.HttpContext.Request,
            context.CancellationToken);
        if (request?.Method is null)
        {
            return WireDecision.Continue;
        }

        var parameters = context.Session.GetParams<CatalogPaginationParams>();
        var method = parameters.Facet switch
        {
            CatalogFacetNames.Prompts => McpMethods.PromptsList,
            _ => McpMethods.ResourcesList,
        };

        if (!string.Equals(request.Method, method, StringComparison.Ordinal))
        {
            return WireDecision.Continue;
        }

        var invocationKey = parameters.Facet == CatalogFacetNames.Prompts
            ? ScenarioWireInvocationKeys.PromptsList
            : ScenarioWireInvocationKeys.ResourcesList;
        var pageIndex = context.Session.IncrementInvocationCount(invocationKey) - 1;
        var pageSize = Math.Max(1, parameters.PageSize);
        var totalItems = Math.Max(0, parameters.TotalItems);

        if (parameters.Fault == CatalogPaginationFaults.ErrorAfterPageN
            && pageIndex + 1 >= Math.Max(1, parameters.FaultAtPage))
        {
            return WireDecision.RespondWithBody(
                StatusCodes.Status200OK,
                ScenarioWireJsonRpc.Error(request.Id, -32603, "Internal error after page"));
        }

        var startIndex = pageIndex * pageSize;
        if (startIndex >= totalItems)
        {
            return WireDecision.RespondWithBody(
                StatusCodes.Status200OK,
                BuildListResult(request.Id, parameters.Facet, [], nextCursor: null));
        }

        var endIndex = Math.Min(startIndex + pageSize, totalItems);
        var items = Enumerable.Range(startIndex, endIndex - startIndex)
            .Select(i => BuildItem(parameters.Facet, i))
            .ToList();

        string? nextCursor;
        if (parameters.Fault == CatalogPaginationFaults.CursorLoop)
        {
            nextCursor = "loop";
        }
        else if (parameters.Fault == CatalogPaginationFaults.DuplicatePage)
        {
            nextCursor = pageIndex == 0 ? "page-1" : null;
            if (pageIndex > 0)
            {
                startIndex = 0;
                endIndex = Math.Min(pageSize, totalItems);
                items = Enumerable.Range(startIndex, endIndex - startIndex)
                    .Select(i => BuildItem(parameters.Facet, i))
                    .ToList();
            }
        }
        else
        {
            nextCursor = endIndex < totalItems ? $"page-{pageIndex + 1}" : null;
        }

        return WireDecision.RespondWithBody(
            StatusCodes.Status200OK,
            BuildListResult(request.Id, parameters.Facet, items, nextCursor));
    }

    private static object BuildItem(string facet, int index) =>
        facet == CatalogFacetNames.Prompts
            ? new
            {
                name = $"prompt_{index}",
                description = $"Test prompt {index}",
            }
            : new
            {
                uri = $"test://resource/{index}",
                name = $"resource_{index}",
                mimeType = "text/plain",
            };

    private static string BuildListResult(object? id, string facet, IReadOnlyList<object> items, string? nextCursor)
    {
        if (facet == CatalogFacetNames.Prompts)
        {
            return ScenarioWireJsonRpc.Result(
                id,
                new
                {
                    prompts = items,
                    nextCursor,
                });
        }

        return ScenarioWireJsonRpc.Result(
            id,
            new
            {
                resources = items,
                nextCursor,
            });
    }
}
