using McpTestServer.API.Constants;

namespace McpTestServer.API.Scenarios;

public interface IScenarioSessionFactory
{
    bool TryGetForCaller(string? callerEmail, out ScenarioSession? session);

    ScenarioSession GetOrCreate(string? callerEmail, string scenarioId, string paramsJson);

    void ReleaseForCaller(string? callerEmail);

    IReadOnlyList<ScenarioSession> ListForCaller(string? callerEmail);

    IReadOnlyList<ScenarioSession> ListAll();
}

public sealed class ScenarioSessionFactory(IScenarioSessionRegistry sessionRegistry) : IScenarioSessionFactory
{
    private readonly object _lock = new();
    private readonly Dictionary<string, ScenarioSession> _sessionsByCaller = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IDisposable> _registrationsByCaller = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetForCaller(string? callerEmail, out ScenarioSession? session)
    {
        var key = CallerKey(callerEmail);

        lock (_lock)
        {
            return _sessionsByCaller.TryGetValue(key, out session);
        }
    }

    public ScenarioSession GetOrCreate(string? callerEmail, string scenarioId, string paramsJson)
    {
        var key = CallerKey(callerEmail);

        lock (_lock)
        {
            if (_sessionsByCaller.TryGetValue(key, out var existing)
                && existing.ScenarioId == scenarioId
                && existing.ParamsJson == paramsJson)
            {
                return existing;
            }

            ReplaceCallerSession(key, existing);
            var session = new ScenarioSession(scenarioId, paramsJson, callerEmail, mcpSessionId: null);
            _sessionsByCaller[key] = session;
            _registrationsByCaller[key] = sessionRegistry.Register(Guid.NewGuid().ToString("N"), session);
            return session;
        }
    }

    public void ReleaseForCaller(string? callerEmail)
    {
        var key = CallerKey(callerEmail);

        lock (_lock)
        {
            ReplaceCallerSession(key, session: null);
        }
    }

    public IReadOnlyList<ScenarioSession> ListForCaller(string? callerEmail)
    {
        var key = CallerKey(callerEmail);

        lock (_lock)
        {
            return _sessionsByCaller.TryGetValue(key, out var session)
                ? [session]
                : Array.Empty<ScenarioSession>();
        }
    }

    public IReadOnlyList<ScenarioSession> ListAll()
    {
        lock (_lock)
        {
            return _sessionsByCaller.Values.ToList();
        }
    }

    private void ReplaceCallerSession(string key, ScenarioSession? session)
    {
        if (_registrationsByCaller.Remove(key, out var registration))
        {
            registration.Dispose();
        }

        if (_sessionsByCaller.Remove(key, out var existing) || session is not null)
        {
            var toDispose = existing ?? session;
            if (toDispose is not null)
            {
                sessionRegistry.Unregister(toDispose);
                toDispose.Dispose();
            }
        }
    }

    private static string CallerKey(string? callerEmail) =>
        string.IsNullOrWhiteSpace(callerEmail)
            ? ScenarioSessionCallerKeys.AnonymousCallerKey
            : callerEmail;
}
