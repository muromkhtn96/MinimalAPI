using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new CategoryId(value));

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        // Unique index trên Name — không cho trùng tên danh mục
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.Description)
            .HasColumnName("description");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        builder.Ignore(c => c.DomainEvents);
    }
}
