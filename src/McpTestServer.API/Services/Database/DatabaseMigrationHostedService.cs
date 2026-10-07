using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpTestServer.API.Services.Database;

public sealed class DatabaseMigrationHostedService(
    IServiceProvider services,
    IDatabaseReadiness databaseReadiness,
    IHostApplicationLifetime applicationLifetime,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        applicationLifetime.ApplicationStarted.Register(OnApplicationStarted);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OnApplicationStarted()
    {
        try
        {
            DatabaseMigrationService.ApplyMigrationsAsync(services).GetAwaiter().GetResult();
            databaseReadiness.MarkReady();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed during startup");
            applicationLifetime.StopApplication();
        }
    }
}
