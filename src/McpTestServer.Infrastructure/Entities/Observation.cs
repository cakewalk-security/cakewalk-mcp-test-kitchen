namespace McpTestServer.Infrastructure.Entities;

public sealed class Observation
{
    public long Id { get; set; }

    public string? RequestId { get; set; }

    public string? CallerEmail { get; set; }

    public string? ScenarioId { get; set; }

    public string Method { get; set; } = string.Empty;

    public string Phase { get; set; } = string.Empty;

    public long DurationMs { get; set; }

    public int? StatusCode { get; set; }

    public bool WasCancelled { get; set; }

    public string? HeadersJson { get; set; }

    public string? RequestParamsJson { get; set; }

    public string? ClientProtocolVersion { get; set; }

    public DateTime OccurredAt { get; set; }
}
