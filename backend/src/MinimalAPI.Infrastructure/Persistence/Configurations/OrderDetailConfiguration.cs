using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("order_details");

        builder.HasKey(d => d.Id);

        // OrderDetailId ↔ Guid conversion
        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value, 
                value => new OrderDetailId(value));

        // OrderId conversion
        builder.Property(d => d.OrderId)
            .HasColumnName("order_id")
            .HasConversion(
                id => id.Value, 
                value => new OrderId(value));

        // Khóa ngoại liên kết với Product
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProductId conversion
        builder.Property(d => d.ProductId)
            .HasColumnName("product_id")
            .HasConversion(
                id => id.Value, 
                value => new ProductId(value));

        builder.Property(d => d.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        // Money — ComplexProperty (Value Object owned)
        builder.ComplexProperty(d => d.UnitPrice, b =>
        {
            b.Property(m => m.Amount).HasColumnName("unit_price_amount").HasColumnType("decimal(18,2)").IsRequired();
            b.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(3).IsRequired();
        });

        builder.Ignore(d => d.LineTotal);
    }
}