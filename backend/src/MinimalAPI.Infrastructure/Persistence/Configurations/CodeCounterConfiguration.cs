using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

internal sealed class CodeCounterConfiguration : IEntityTypeConfiguration<CodeCounter>
{
    public void Configure(EntityTypeBuilder<CodeCounter> builder)
    {
        builder.ToTable("code_counters");

        builder.HasKey(c => c.Prefix);

        builder.Property(c => c.Prefix)
            .HasColumnName("prefix")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.CurrentValue)
            .HasColumnName("current_value")
            .IsRequired();
    }
}
