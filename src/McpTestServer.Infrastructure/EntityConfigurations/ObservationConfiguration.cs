using McpTestServer.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace McpTestServer.Infrastructure.EntityConfigurations;

internal sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("Observations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RequestId).HasMaxLength(128);
        builder.Property(e => e.CallerEmail).HasMaxLength(320);
        builder.Property(e => e.ScenarioId).HasMaxLength(128);
        builder.Property(e => e.Method).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Phase).HasMaxLength(128).IsRequired();
        builder.Property(e => e.DurationMs).IsRequired();
        builder.Property(e => e.ClientProtocolVersion).HasMaxLength(32);
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => e.OccurredAt);
    }
}
