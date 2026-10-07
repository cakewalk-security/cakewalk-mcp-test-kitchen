using McpTestServer.Infrastructure.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace McpTestServer.Infrastructure;

public sealed class McpTestServerContext(DbContextOptions<McpTestServerContext> options) : DbContext(options)
{
    public const string DefaultSchema = "api";
    public const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    public DbSet<Entities.UserScenarioSelection> UserScenarioSelections => Set<Entities.UserScenarioSelection>();

    public DbSet<Entities.Observation> Observations => Set<Entities.Observation>();

    public DbSet<Entities.UserMcpPat> UserMcpPats => Set<Entities.UserMcpPat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new UserScenarioSelectionConfiguration());
        modelBuilder.ApplyConfiguration(new ObservationConfiguration());
        modelBuilder.ApplyConfiguration(new UserMcpPatConfiguration());
    }
}
