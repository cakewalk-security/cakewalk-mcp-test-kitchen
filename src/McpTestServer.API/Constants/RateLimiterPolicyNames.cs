namespace McpTestServer.API.Constants;

public static class RateLimiterPolicyNames
{
    public const string PatEndpoints = "PatEndpointsRateLimiter";

    public const string FeedbackEndpoint = "FeedbackEndpointRateLimiter";

    public const string EraseEndpoint = "EraseEndpointRateLimiter";
}

public static class RateLimiterLimits
{
    public const int McpEndpointPermitLimit = 120;

    public const int McpEndpointWindowMinutes = 1;

    public const int PatEndpointsPermitLimit = 30;

    public const int PatEndpointsWindowMinutes = 1;

    public const int FeedbackEndpointPermitLimit = 5;

    public const int FeedbackEndpointWindowMinutes = 10;

    public const int EraseEndpointPermitLimit = 5;

    public const int EraseEndpointWindowMinutes = 10;
}

public static class RateLimiterPartitionKeys
{
    public const string Unknown = "unknown";
}
