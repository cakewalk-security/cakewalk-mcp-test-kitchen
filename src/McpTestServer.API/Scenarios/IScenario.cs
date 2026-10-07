using System.Text.Json;
using McpTestServer.API.Constants;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios;

public interface IScenario
{
    ScenarioMetadata Metadata { get; }

    void ConfigureSession(McpServerOptions options, ScenarioSession session);

    ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken);

    ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context);
}

public abstract class ScenarioBase : IScenario
{
    public abstract ScenarioMetadata Metadata { get; }

    public virtual void ConfigureSession(McpServerOptions options, ScenarioSession session) =>
        ScenarioSessionSetup.ConfigureSingleRunTool(options, session, this);

    public virtual ValueTask<CallToolResult> RunToolAsync(
        ScenarioSession session,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var invocation = session.IncrementInvocationCount(McpToolNames.RunScenario);
        var message = ReadOptionalMessage(arguments);
        return ValueTask.FromResult(ScenarioToolResults.Success(session, invocation, message: message));
    }

    public virtual ValueTask<WireDecision> OnRequestAsync(ScenarioWireContext context) =>
        ValueTask.FromResult(WireDecision.Continue);

    protected static string? ReadOptionalMessage(IReadOnlyDictionary<string, JsonElement>? arguments)
    {
        if (arguments?.TryGetValue("message", out var messageElement) == true)
        {
            return messageElement.GetString();
        }

        return null;
    }
}
