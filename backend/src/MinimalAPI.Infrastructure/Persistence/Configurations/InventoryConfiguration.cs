using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventories");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new InventoryId(value));

        builder.Property(i => i.ProductId)
            .HasColumnName("product_id")
            .HasConversion(id => id.Value, value => new ProductId(value));

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(i => i.ProductId)
            .IsUnique();

        builder.HasOne(i => i.Product)
            .WithOne()
            .HasForeignKey<Inventory>(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(i => i.DomainEvents);
    }
}
