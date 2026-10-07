namespace McpTestServer.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public const string ConnectionStringKey = "CONNECTION_STRING";

    public string CONNECTION_STRING { get; set; } =
        "Host=localhost;Port=15433;Database=mcp_test_server;Username=postgres;Password=postgres";
}
