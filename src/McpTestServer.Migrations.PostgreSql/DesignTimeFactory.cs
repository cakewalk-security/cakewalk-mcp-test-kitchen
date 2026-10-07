using McpTestServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace McpTestServer.Migrations.PostgreSql;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<McpTestServerContext>
{
    public McpTestServerContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Port=15433;Database=mcp_test_server;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<McpTestServerContext>();
        optionsBuilder.UseNpgsql(connectionString, o =>
        {
            o.MigrationsAssembly(typeof(DesignTimeFactory).Assembly.GetName().Name);
            o.MigrationsHistoryTable(McpTestServerContext.MigrationsHistoryTableName, McpTestServerContext.DefaultSchema);
        });

        return new McpTestServerContext(optionsBuilder.Options);
    }
}
