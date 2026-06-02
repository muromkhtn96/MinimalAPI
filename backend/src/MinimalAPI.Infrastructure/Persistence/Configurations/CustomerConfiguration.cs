using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình mapping dữ liệu cho thực thể Customer.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <summary>
    /// Cấu hình bảng, khóa chính, thuộc tính và index cho Customer.
    /// </summary>
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        /// Đặt tên bảng
        builder.ToTable("Customers");

        // Khóa chính
        builder.HasKey(c => c.Id);

        // Cấu hình Id với Value Object CustomerId
        builder.Property(c => c.Id)
            .HasColumnName("Id")
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value));

        // Mã khách hàng
        builder.Property(c => c.Code)
            .HasColumnName("Code")
            .HasMaxLength(20)
            .IsRequired();

        // Tạo unique index cho mã khách hàng
        builder.HasIndex(c => c.Code)
            .IsUnique();

        // Họ tên khách hàng
        builder.Property(c => c.FullName)
            .HasColumnName("FullName")
            .HasMaxLength(200)
            .IsRequired();

        // Tạo index hỗ trợ tìm kiếm theo tên
        builder.HasIndex(c => c.FullName);

        // Số điện thoại khách hàng
        builder.Property(c => c.Phone)
            .HasColumnName("Phone")
            .HasMaxLength(20);

        // Tạo index hỗ trợ tìm kiếm theo số điện thoại
        builder.HasIndex(c => c.Phone);
    }
}