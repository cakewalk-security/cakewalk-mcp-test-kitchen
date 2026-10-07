using McpTestServer.API.Options;
using McpTestServer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpTestServer.API.Services.Observations;

public sealed class ObservationRetentionService(
    IServiceScopeFactory scopeFactory,
    IOptions<ObservationRetentionOptions> options,
    ILogger<ObservationRetentionService> logger) : BackgroundService
{
    private static readonly TimeSpan PruneInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PruneInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PruneAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Observation retention prune failed.");
            }
        }
    }

    private async Task PruneAsync(CancellationToken cancellationToken)
    {
        var retentionOptions = options.Value;
        if (retentionOptions.RetentionHours <= 0 && retentionOptions.MaxRows is null or <= 0)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpTestServerContext>();

        if (retentionOptions.RetentionHours > 0)
        {
            var cutoff = DateTime.UtcNow.AddHours(-retentionOptions.RetentionHours);
            var deletedByAge = await db.Observations
                .Where(o => o.OccurredAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedByAge > 0)
            {
                logger.LogInformation("Pruned {DeletedCount} observations older than retention cutoff.", deletedByAge);
            }
        }

        if (retentionOptions.MaxRows is > 0)
        {
            var totalCount = await db.Observations.CountAsync(cancellationToken);
            var overflow = totalCount - retentionOptions.MaxRows.Value;
            if (overflow > 0)
            {
                var keepIds = db.Observations
                    .OrderByDescending(o => o.OccurredAt)
                    .Take(retentionOptions.MaxRows.Value)
                    .Select(o => o.Id);

                var deletedByCount = await db.Observations
                    .Where(o => !keepIds.Contains(o.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                if (deletedByCount > 0)
                {
                    logger.LogInformation("Pruned {DeletedCount} observations to enforce max row limit.", deletedByCount);
                }
            }
        }
    }
}
