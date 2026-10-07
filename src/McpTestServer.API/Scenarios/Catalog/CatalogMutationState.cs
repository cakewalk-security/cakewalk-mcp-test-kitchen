using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTestServer.API.Scenarios.Catalog;

public sealed class CatalogMutationState : IDisposable
{
    private readonly List<Timer> _timers = [];
    private bool _disposed;

    public CatalogMutationState(
        ScenarioSession session,
        McpServerPrimitiveCollection<McpServerTool> tools,
        IScenario runScenarioHost)
    {
        Session = session;
        Tools = tools;
        RunScenarioHost = runScenarioHost;
        Tools.Add(new RunScenarioMcpServerTool(session, runScenarioHost));
    }

    public ScenarioSession Session { get; }

    public McpServerPrimitiveCollection<McpServerTool> Tools { get; }

    public IScenario RunScenarioHost { get; }

    public void ScheduleMutations(CatalogMutationParams parameters)
    {
        foreach (var mutation in parameters.Mutations ?? [])
        {
            var timer = new Timer(
                _ => ApplyMutation(mutation),
                state: null,
                dueTime: Math.Max(0, mutation.AfterMs),
                period: global::System.Threading.Timeout.Infinite);
            _timers.Add(timer);
        }
    }

    private void ApplyMutation(CatalogMutationEntry mutation)
    {
        switch (mutation.Action)
        {
            case CatalogMutationActions.Add:
                Tools.TryAdd(new StubMcpServerTool(mutation.ToolName, $"Catalog mutation tool {mutation.ToolName}"));
                EmitAdditionalNotifications(mutation, mutation.ToolName);
                break;
            case CatalogMutationActions.Remove:
                if (Tools.TryGetPrimitive(mutation.ToolName, out var existing) && existing is not null)
                {
                    Tools.Remove(existing);
                    EmitAdditionalNotifications(mutation, mutation.ToolName);
                }

                break;
            case CatalogMutationActions.Rename:
                if (Tools.TryGetPrimitive(mutation.ToolName, out var toRename) && toRename is not null
                    && !string.IsNullOrWhiteSpace(mutation.NewName))
                {
                    Tools.Remove(toRename);
                    Tools.TryAdd(new StubMcpServerTool(mutation.NewName, $"Renamed from {mutation.ToolName}"));
                    EmitAdditionalNotifications(mutation, mutation.NewName!);
                }

                break;
        }
    }

    private void EmitAdditionalNotifications(CatalogMutationEntry mutation, string baseName)
    {
        var extraNotifications = Math.Max(1, mutation.ListChangedNotifications) - 1;
        for (var i = 0; i < extraNotifications; i++)
        {
            var transientName = $"{baseName}_burst_{i}";
            if (Tools.TryAdd(new StubMcpServerTool(transientName, "Transient burst notification tool")))
            {
                Tools.Remove(Tools[transientName]);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var timer in _timers)
        {
            timer.Dispose();
        }

        _timers.Clear();
    }
}
