using McpTestServer.Infrastructure;
using McpTestServer.Infrastructure.Options;
using McpTestServer.Migrations.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpTestServer.API.Services.Database;

public static class DatabaseMigrationService
{
    public static async Task ApplyMigrationsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<McpTestServerContext>>();
        var dbOptions = scope.ServiceProvider.GetRequiredService<DbContextOptions<McpTestServerContext>>();

        logger.LogInformation("Applying {Context} migrations", nameof(McpTestServerContext));

        await using var context = new McpTestServerContext(dbOptions);
        await context.Database.MigrateAsync(cancellationToken);
    }

    public static void AddMcpTestServerDatabase(this IServiceCollection services)
    {
        services.AddDbContext<McpTestServerContext>((sp, options) =>
        {
            var databaseOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(databaseOptions.CONNECTION_STRING, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(DesignTimeFactory).Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable(
                    McpTestServerContext.MigrationsHistoryTableName,
                    McpTestServerContext.DefaultSchema);
            });
        });
    }
}
