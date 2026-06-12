using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình Entity Framework Core cho thực thể Order.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>
    /// Cấu hình bảng, khóa chính, các thuộc tính và quan hệ cho thực thể Order.
    /// </summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        /// Bảng "orders"
        builder.ToTable("orders");

        // Khóa chính
        builder.HasKey(o => o.Id);

        // Cấu hình OrderId với conversion
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value, 
                value => new OrderId(value));

        // Cấu hình Code
        builder.Property(o => o.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(o => o.Code)
            .IsUnique();

        // Cấu hình CustomerId với conversion
        builder.Property(o => o.CustomerId)
            .HasColumnName("customer_id")
            .HasConversion(
                id => id.Value, 
                value => new CustomerId(value));

        // Cấu hình Status
        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<short>();

        // Cấu hình TotalAmount
        builder.ComplexProperty(o => o.TotalAmount, b =>
        {
            b.Property(m => m.Amount).HasColumnName("total_amount").HasColumnType("decimal(18,2)").IsRequired();
            b.Property(m => m.Currency).HasColumnName("total_currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(o => o.Note).HasColumnName("note");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");

        // Khóa ngoại liên kết với Customer
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship 1 - N with OrderDetail (Cascade Delete)
        builder.HasMany(o => o.Details)
            .WithOne()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF Core backing field mapping for _details
        builder.Metadata
            .FindNavigation(nameof(Order.Details))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Ignore DomainEvents
        builder.Ignore(o => o.DomainEvents);
    }
}