namespace McpTestServer.API.Scenarios;

public interface IScenarioCatalog
{
    IReadOnlyList<ScenarioMetadata> ListMetadata();

    IScenario GetRequired(string scenarioId);

    bool TryGet(string scenarioId, out IScenario? scenario);

    void ValidateParams<T>(string paramsJson) where T : new();
}

public sealed class ScenarioCatalog : IScenarioCatalog
{
    private readonly Dictionary<string, IScenario> _scenarios = new(StringComparer.Ordinal);

    public void Register(IScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var id = scenario.Metadata.Id;
        if (_scenarios.ContainsKey(id))
        {
            throw new InvalidOperationException($"Scenario '{id}' is already registered.");
        }

        _scenarios[id] = scenario;
    }

    public IReadOnlyList<ScenarioMetadata> ListMetadata() =>
        _scenarios.Values.Select(s => s.Metadata).OrderBy(m => m.Id, StringComparer.Ordinal).ToList();

    public IScenario GetRequired(string scenarioId)
    {
        if (!_scenarios.TryGetValue(scenarioId, out var scenario))
        {
            throw new KeyNotFoundException($"Scenario '{scenarioId}' is not registered.");
        }

        return scenario;
    }

    public bool TryGet(string scenarioId, out IScenario? scenario) =>
        _scenarios.TryGetValue(scenarioId, out scenario);

    public void ValidateParams<T>(string paramsJson) where T : new()
    {
        if (!ScenarioParamsBinder.TryBind<T>(paramsJson, out _, out var error))
        {
            throw new ArgumentException(error ?? "Invalid params JSON.", nameof(paramsJson));
        }
    }
}
