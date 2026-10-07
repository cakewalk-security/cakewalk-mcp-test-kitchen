namespace McpTestServer.API.Scenarios;

public interface IScenarioSessionRegistry
{
    IDisposable Register(string mcpSessionId, ScenarioSession session);

    bool TryGet(string? mcpSessionId, out ScenarioSession? session);

    IReadOnlyList<ScenarioSessionInfo> ListForCaller(string callerEmail);

    void TerminateSessionsForCaller(string callerEmail);

    void Unregister(ScenarioSession session);
}

public sealed record ScenarioSessionInfo(
    string? McpSessionId,
    string ScenarioId,
    string ParamsJson,
    DateTime StartedAt,
    IReadOnlyDictionary<string, int> InvocationCounts);

public sealed class ScenarioSessionRegistry : IScenarioSessionRegistry
{
    // Clients pick session IDs, so cap how many one session can accumulate.
    public const int MaxSessionIdsPerSession = 32;

    private readonly object _lock = new();
    private readonly Dictionary<ScenarioSession, List<string>> _sessionIdsBySession = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, ScenarioSession> _sessionsByMcpSessionId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _sessionIdsByCallerEmail = new(StringComparer.OrdinalIgnoreCase);

    public IDisposable Register(string mcpSessionId, ScenarioSession session)
    {
        lock (_lock)
        {
            RemoveFromMaps(mcpSessionId);
            _sessionsByMcpSessionId[mcpSessionId] = session;
            session.SetMcpSessionId(mcpSessionId);

            if (!_sessionIdsBySession.TryGetValue(session, out var idsForSession))
            {
                idsForSession = [];
                _sessionIdsBySession[session] = idsForSession;
            }

            idsForSession.Add(mcpSessionId);
            while (idsForSession.Count > MaxSessionIdsPerSession)
            {
                RemoveFromMaps(idsForSession[0]);
            }

            if (!string.IsNullOrWhiteSpace(session.CallerEmail))
            {
                if (!_sessionIdsByCallerEmail.TryGetValue(session.CallerEmail, out var sessionIds))
                {
                    sessionIds = new HashSet<string>(StringComparer.Ordinal);
                    _sessionIdsByCallerEmail[session.CallerEmail] = sessionIds;
                }

                sessionIds.Add(mcpSessionId);
            }
        }

        return new SessionRegistration(this, mcpSessionId);
    }

    public bool TryGet(string? mcpSessionId, out ScenarioSession? session)
    {
        session = null;
        if (string.IsNullOrWhiteSpace(mcpSessionId))
        {
            return false;
        }

        lock (_lock)
        {
            return _sessionsByMcpSessionId.TryGetValue(mcpSessionId, out session);
        }
    }

    public IReadOnlyList<ScenarioSessionInfo> ListForCaller(string callerEmail)
    {
        lock (_lock)
        {
            if (!_sessionIdsByCallerEmail.TryGetValue(callerEmail, out var sessionIds))
            {
                return Array.Empty<ScenarioSessionInfo>();
            }

            return sessionIds
                .Select(id => _sessionsByMcpSessionId.TryGetValue(id, out var session) ? session : null)
                .Where(session => session is not null)
                .Select(session => new ScenarioSessionInfo(
                    session!.McpSessionId,
                    session.ScenarioId,
                    session.ParamsJson,
                    session.StartedAt,
                    session.GetInvocationCountsSnapshot()))
                .ToList();
        }
    }

    public void TerminateSessionsForCaller(string callerEmail)
    {
        List<ScenarioSession> sessions;
        lock (_lock)
        {
            if (!_sessionIdsByCallerEmail.TryGetValue(callerEmail, out var sessionIds))
            {
                return;
            }

            sessions = sessionIds
                .Select(id => _sessionsByMcpSessionId.TryGetValue(id, out var session) ? session : null)
                .Where(session => session is not null)
                .Cast<ScenarioSession>()
                .Distinct()
                .ToList();
        }

        foreach (var session in sessions)
        {
            session.Cancel();
        }
    }

    public void Unregister(ScenarioSession session)
    {
        lock (_lock)
        {
            var ids = _sessionsByMcpSessionId
                .Where(entry => ReferenceEquals(entry.Value, session))
                .Select(entry => entry.Key)
                .ToList();

            foreach (var id in ids)
            {
                RemoveFromMaps(id);
            }
        }
    }

    private void UnregisterById(string mcpSessionId)
    {
        lock (_lock)
        {
            RemoveFromMaps(mcpSessionId);
        }
    }

    private void RemoveFromMaps(string mcpSessionId)
    {
        if (!_sessionsByMcpSessionId.Remove(mcpSessionId, out var session))
        {
            return;
        }

        if (_sessionIdsBySession.TryGetValue(session, out var idsForSession))
        {
            idsForSession.Remove(mcpSessionId);
            if (idsForSession.Count == 0)
            {
                _sessionIdsBySession.Remove(session);
            }
        }

        if (!string.IsNullOrWhiteSpace(session.CallerEmail)
            && _sessionIdsByCallerEmail.TryGetValue(session.CallerEmail, out var sessionIds))
        {
            sessionIds.Remove(mcpSessionId);
            if (sessionIds.Count == 0)
            {
                _sessionIdsByCallerEmail.Remove(session.CallerEmail);
            }
        }
    }

    private sealed class SessionRegistration(ScenarioSessionRegistry registry, string mcpSessionId) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            registry.UnregisterById(mcpSessionId);
        }
    }
}
