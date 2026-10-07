namespace McpTestServer.API.Constants;

public static class McpObservationConstants
{
    public const int RequestBodyBufferLimitBytes = 65536;

    public const int SseHeartbeatIntervalSeconds = 30;

    public const int SseChannelCapacity = 256;

    public const string SseHeartbeatComment = ": heartbeat\n\n";

    public const string SseCacheControl = "no-cache, no-transform";

    public const string SseAccelBufferingHeader = "X-Accel-Buffering";

    public const string SseAccelBufferingDisabled = "no";

    /// <summary>
    /// Shared cap for Kestrel and the /mcp observation buffer so this public POST
    /// cannot accept Kestrel's ~30 MB default.
    /// </summary>
    public const int MaxRequestBodyBytes = RequestBodyBufferLimitBytes;
}
