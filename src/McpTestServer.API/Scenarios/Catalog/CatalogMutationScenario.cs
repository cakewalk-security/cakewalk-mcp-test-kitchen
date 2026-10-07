using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class CatalogMutationScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.CatalogMutation,
        "Catalog mutation",
        "Dynamically adds, removes, or renames tools after configured delays and emits tools/list_changed notifications.",
        ScenarioAreas.Catalog,
        """
        {
          "mutations": [
            {"action":"add","toolName":"extra_tool","afterMs":1000,"listChangedNotifications":1}
          ]
        }
        """);

    public override void ConfigureSession(McpServerOptions options, ScenarioSession session)
    {
        var existingState = session.GetState<CatalogMutationState>();
        if (existingState is not null)
        {
            ApplySessionOptions(options, existingState.Tools);
            return;
        }

        var tools = new McpServerPrimitiveCollection<McpServerTool>();
        var state = new CatalogMutationState(session, tools, this);
        session.SetState(state);
        state.ScheduleMutations(session.GetParams<CatalogMutationParams>());
        ApplySessionOptions(options, tools);
    }

    private static void ApplySessionOptions(
        McpServerOptions options,
        McpServerPrimitiveCollection<McpServerTool> tools)
    {
        options.ToolCollection = tools;
        options.Capabilities = new ServerCapabilities
        {
            Tools = new ToolsCapability { ListChanged = true },
        };
        ScenarioSessionSetup.ApplyBaselineCatalog(options);
    }
}
