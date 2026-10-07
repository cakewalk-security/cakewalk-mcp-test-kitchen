using McpTestServer.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace McpTestServer.Infrastructure.EntityConfigurations;

internal sealed class UserMcpPatConfiguration : IEntityTypeConfiguration<UserMcpPat>
{
    public void Configure(EntityTypeBuilder<UserMcpPat> builder)
    {
        builder.ToTable("UserMcpPats");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email).HasMaxLength(320).IsRequired();
        builder.Property(e => e.PatHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ProtectedPat).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.PatHash);
    }
}
