using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using McpTestServer.API.Options;
using McpTestServer.Infrastructure.Entities;

namespace McpTestServer.API.Controllers.Management.Models;

public sealed record CurrentUserResponse(
    string? Email,
    bool GoogleLoginEnabled,
    bool GitHubLoginEnabled,
    string McpProtocolVersion,
    bool McpStateless,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool CanAccessAdmin = false);

public sealed record AdminUsageTotalsResponse(
    int RegisteredUserCount,
    int ConnectedUserCount,
    int LiveSessionCount,
    int ActiveUsersLast24Hours,
    int ObservationsLast24Hours,
    int ObservationsLast7Days);

public sealed record AdminUsageUserRow(
    string MaskedEmail,
    DateTime? RegisteredAt,
    DateTime? LastSeenAt,
    int ObservationCountLast24Hours,
    int ObservationCountLast7Days,
    int LiveSessionCount,
    string? LastScenarioId);

public sealed record AdminUsageResponse(
    AdminUsageTotalsResponse Totals,
    IReadOnlyList<AdminUsageUserRow> Users);

public sealed record UserMcpPatResponse(string Email, string Pat);

public sealed record UserScenarioSelectionResponse(
    string ScenarioId,
    string ParamsJson,
    DateTime UpdatedAt,
    string AppliesToMessage)
{
    public const string AppliesToNextSessionMessage = "Applies to your next MCP session.";
}

public sealed record SubmitFeedbackRequest
{
    [Required]
    [MaxLength(FeedbackOptions.MaxMessageLength)]
    public string Message { get; init; } = string.Empty;
}

public sealed record EraseUserDataRequest
{
    [Required]
    [MaxLength(64)]
    public string Confirmation { get; init; } = string.Empty;
}

public sealed record SetUserScenarioSelectionRequest
{
    [Required]
    [MaxLength(128)]
    public string ScenarioId { get; init; } = string.Empty;

    [Required]
    [MaxLength(8192)]
    public string ParamsJson { get; init; } = "{}";
}

public sealed record ScenarioSessionResponse(
    string? McpSessionId,
    string ScenarioId,
    string ParamsJson,
    DateTime StartedAt,
    IReadOnlyDictionary<string, int> InvocationCounts);

public sealed record ObservationResponse(
    long Id,
    string? RequestId,
    string? CallerEmail,
    string? ScenarioId,
    string Method,
    string Phase,
    long DurationMs,
    int? StatusCode,
    bool WasCancelled,
    string? HeadersJson,
    string? RequestParamsJson,
    string? ClientProtocolVersion,
    DateTime OccurredAt)
{
    public static ObservationResponse FromEntity(Observation observation) =>
        new(
            observation.Id,
            observation.RequestId,
            observation.CallerEmail,
            observation.ScenarioId,
            observation.Method,
            observation.Phase,
            observation.DurationMs,
            observation.StatusCode,
            observation.WasCancelled,
            observation.HeadersJson,
            observation.RequestParamsJson,
            observation.ClientProtocolVersion,
            observation.OccurredAt);
}

public sealed record ObservationListResponse(
    IReadOnlyList<ObservationResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record RuntimeStateResponse(
    string ScenarioId,
    string ParamsJson,
    IReadOnlyList<ScenarioSessionResponse> LiveSessions);
