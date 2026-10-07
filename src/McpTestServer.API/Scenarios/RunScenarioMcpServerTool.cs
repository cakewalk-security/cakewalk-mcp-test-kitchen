using System.Text.Json;
using McpTestServer.API.Constants;
using McpTestServer.API.Scenarios.Baseline;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios;

public sealed class RunScenarioMcpServerTool(ScenarioSession session, IScenario scenario) : McpServerTool
{
    private static readonly Tool ProtocolToolDefinition = new()
    {
        Name = McpToolNames.RunScenario,
        Description =
            "MCP Test Kitchen fixture. Runs the active test scenario configured for your user in the admin UI. " +
            "Behavior depends on the saved scenario and params (for example delays, hangs, or HTTP error sequences).",
        InputSchema = JsonSerializer.Deserialize<JsonElement>("""
            {
              "type": "object",
              "properties": {
                "message": {
                  "type": "string",
                  "description": "Optional label included in the response for debugging."
                }
              }
            }
            """),
    };

    public override Tool ProtocolTool => ProtocolToolDefinition;

    public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        return scenario.RunToolAsync(
            session,
            request.Params.Arguments as IReadOnlyDictionary<string, JsonElement>,
            request,
            cancellationToken);
    }
}

internal static class ScenarioSessionSetup
{
    public static void ConfigureSingleRunTool(McpServerOptions options, ScenarioSession session, IScenario scenario)
    {
        options.ToolCollection = new McpServerPrimitiveCollection<McpServerTool>
        {
            new RunScenarioMcpServerTool(session, scenario),
        };
        ApplyBaselineCatalog(options);
    }

    public static void ApplyBaselineCatalog(McpServerOptions options)
    {
        var primitives = new BaselineCatalogPrimitives();
        options.ResourceCollection = new McpServerResourceCollection
        {
            McpServerResource.Create(primitives.ValidResource),
            McpServerResource.Create(primitives.InvalidResourcePlaceholder),
        };
        options.PromptCollection = new McpServerPrimitiveCollection<McpServerPrompt>
        {
            McpServerPrompt.Create(primitives.ValidPrompt),
            McpServerPrompt.Create(primitives.InvalidPromptPlaceholder),
        };
    }
}
