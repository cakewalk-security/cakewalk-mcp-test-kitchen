namespace McpTestServer.API.Options;

public sealed class ObservationRetentionOptions
{
    public const string RetentionHoursEnv = "MCP_TEST_SERVER_OBSERVATION_RETENTION_HOURS";

    public const string MaxRowsEnv = "MCP_TEST_SERVER_OBSERVATION_MAX_ROWS";

    public const int DefaultRetentionHours = 168;

    public const int DefaultMaxRows = 100_000;

    public int RetentionHours { get; set; } = DefaultRetentionHours;

    public int? MaxRows { get; set; } = DefaultMaxRows;
}
