using McpTestServer.API.Constants;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Errors;

public sealed class ToolErrorScenario : ScenarioBase
{
    public override ScenarioMetadata Metadata { get; } = new(
        ScenarioIds.ErrorsToolError,
        "Tool error (isError)",
        "Returns a valid MCP tool result with isError=true on a configured invocation (application layer).",
        ScenarioAreas.Errors,
        """{"errorMessage":"Tool-side failure","onInvocation":1}""");

    public override ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var parameters = session.GetParams<ToolErrorParams>();
        if (ScenarioWireHelpers.ShouldApplyOnInvocation(invocation, parameters.OnInvocation))
        {
            return ValueTask.FromResult(ScenarioToolResults.ToolError(
                session,
                invocation,
                parameters.ErrorMessage,
                ReadOptionalMessage(arguments)));
        }

        return ValueTask.FromResult(ScenarioToolResults.Success(
            session,
            invocation,
            message: ReadOptionalMessage(arguments)));
    }
}
