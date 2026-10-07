namespace McpTestServer.API.Scenarios;

public enum WireDecisionKind
{
    Continue,
    RespondWithStatus,
    RespondWithBody,
    CloseConnection,
}

public sealed record WireDecision(
    WireDecisionKind Kind,
    int? StatusCode = null,
    string? Body = null,
    int? DelayMs = null,
    bool HonorCancellationDuringDelay = true)
{
    public static WireDecision Continue { get; } = new(WireDecisionKind.Continue);

    public static WireDecision RespondWithStatus(int statusCode) =>
        new(WireDecisionKind.RespondWithStatus, StatusCode: statusCode);

    public static WireDecision RespondWithBody(int statusCode, string body) =>
        new(WireDecisionKind.RespondWithBody, StatusCode: statusCode, Body: body);

    public static WireDecision CloseConnection() => new(WireDecisionKind.CloseConnection);

    public static WireDecision Delay(int delayMs, bool honorCancellation = true) =>
        new(WireDecisionKind.Continue, DelayMs: delayMs, HonorCancellationDuringDelay: honorCancellation);
}
