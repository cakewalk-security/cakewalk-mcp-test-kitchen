using McpTestServer.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace McpTestServer.Infrastructure.EntityConfigurations;

internal sealed class UserScenarioSelectionConfiguration : IEntityTypeConfiguration<UserScenarioSelection>
{
    public void Configure(EntityTypeBuilder<UserScenarioSelection> builder)
    {
        builder.ToTable("UserScenarioSelections");

        builder.HasKey(e => e.Email);

        builder.Property(e => e.Email).HasMaxLength(320).IsRequired();
        builder.Property(e => e.ScenarioId).HasMaxLength(128).IsRequired();
        builder.Property(e => e.ParamsJson).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
    }
}
