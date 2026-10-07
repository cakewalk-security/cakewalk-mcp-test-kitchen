using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpTestServer.API.Scenarios;

public sealed class ScenarioSession : IDisposable
{
    private readonly object _lock = new();
    private readonly Dictionary<string, int> _invocationCounts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object> _state = new(StringComparer.Ordinal);
    private readonly List<HttpContext> _activeRequests = [];
    private bool _disposed;

    public ScenarioSession(string scenarioId, string paramsJson, string? callerEmail, string? mcpSessionId)
    {
        ScenarioId = scenarioId;
        ParamsJson = paramsJson;
        CallerEmail = callerEmail;
        McpSessionId = mcpSessionId;
        SessionCancellation = new CancellationTokenSource();
    }

    public string ScenarioId { get; }

    public string ParamsJson { get; }

    public string? CallerEmail { get; }

    public string? McpSessionId { get; private set; }

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public CancellationTokenSource SessionCancellation { get; }

    public bool BelongsTo(string? callerEmail) =>
        string.IsNullOrWhiteSpace(CallerEmail)
            ? string.IsNullOrWhiteSpace(callerEmail)
            : string.Equals(CallerEmail, callerEmail, StringComparison.OrdinalIgnoreCase);

    public void SetMcpSessionId(string? sessionId)
    {
        McpSessionId = sessionId;
    }

    public void AttachRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        lock (_lock)
        {
            _activeRequests.Add(context);
        }
    }

    public void DetachRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        lock (_lock)
        {
            _activeRequests.Remove(context);
        }
    }

    public void Cancel()
    {
        SessionCancellation.Cancel();
        HttpContext[] toAbort;
        lock (_lock)
        {
            toAbort = [.. _activeRequests];
        }

        foreach (var request in toAbort)
        {
            request.Abort();
        }
    }

    public T GetParams<T>() where T : new()
    {
        return ScenarioParamsBinder.BindOrDefault(ParamsJson, new T());
    }

    public bool TryGetParams<T>(out T value) where T : new()
    {
        value = ScenarioParamsBinder.BindOrDefault(ParamsJson, new T());
        return true;
    }

    public int GetInvocationCount(string key)
    {
        lock (_lock)
        {
            return _invocationCounts.TryGetValue(key, out var count) ? count : 0;
        }
    }

    public int IncrementInvocationCount(string key)
    {
        lock (_lock)
        {
            _invocationCounts.TryGetValue(key, out var count);
            var next = count + 1;
            _invocationCounts[key] = next;
            return next;
        }
    }

    public void ResetInvocationCounts()
    {
        lock (_lock)
        {
            _invocationCounts.Clear();
        }
    }

    public void SetState<T>(T state) where T : class
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_lock)
        {
            var key = typeof(T).FullName!;
            if (_state.TryGetValue(key, out var previous) && !ReferenceEquals(previous, state))
            {
                (previous as IDisposable)?.Dispose();
            }

            _state[key] = state;
        }
    }

    public T? GetState<T>() where T : class
    {
        lock (_lock)
        {
            return _state.TryGetValue(typeof(T).FullName!, out var value) ? value as T : null;
        }
    }

    public IReadOnlyDictionary<string, int> GetInvocationCountsSnapshot()
    {
        lock (_lock)
        {
            return new Dictionary<string, int>(_invocationCounts, StringComparer.Ordinal);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            SessionCancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        object[] stateValues;
        lock (_lock)
        {
            stateValues = [.. _state.Values];
            _state.Clear();
            _activeRequests.Clear();
        }

        foreach (var value in stateValues)
        {
            (value as IDisposable)?.Dispose();
        }

        SessionCancellation.Dispose();
    }
}

public static class ScenarioParamsBinder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public const string ParamsMustBeObject = "Params JSON must be an object.";

    public const string ParamsNotValidJson = "Params JSON is not valid.";

    public static T BindOrDefault<T>(string paramsJson, T defaultValue) where T : new()
    {
        if (string.IsNullOrWhiteSpace(paramsJson) || paramsJson == "{}")
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(paramsJson, SerializerOptions) ?? defaultValue;
        }
        catch (JsonException)
        {
            return defaultValue;
        }
    }

    public static bool TryBind<T>(string paramsJson, out T? value, out string? error) where T : new()
    {
        value = default;
        error = null;

        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            paramsJson = "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(paramsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = ParamsMustBeObject;
                return false;
            }

            value = JsonSerializer.Deserialize<T>(paramsJson, SerializerOptions) ?? new T();
            if (!TryRejectNullCollections(value, out error))
            {
                value = default;
                return false;
            }

            if (value is IScenarioParamsValidation validatable)
            {
                var validationError = validatable.Validate();
                if (validationError is not null)
                {
                    error = validationError;
                    value = default;
                    return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            error = ParamsNotValidJson;
            return false;
        }
    }

    private static bool TryRejectNullCollections<T>(T value, out string? error)
    {
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (!IsCollectionProperty(property.PropertyType))
            {
                continue;
            }

            if (property.GetValue(value) is not null)
            {
                continue;
            }

            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
            error = $"Params JSON must not set '{name}' to null.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsCollectionProperty(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        return typeof(IEnumerable).IsAssignableFrom(type);
    }
}
