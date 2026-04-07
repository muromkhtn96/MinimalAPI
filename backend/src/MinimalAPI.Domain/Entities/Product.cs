using MinimalAPI.Domain.Events;
using MinimalAPI.Domain.Primitives;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Domain.Entities;

/// <summary>
/// Aggregate Root — Sản phẩm.
/// Mọi thay đổi trạng thái phải qua method, KHÔNG public setter.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public ProductName Name { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public CategoryId CategoryId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core cần parameterless constructor
    private Product() { }

    public static Product Create(
        ProductName name,
        Money price,
        CategoryId categoryId,
        string? description)
    {
        var product = new Product
        {
            Id = ProductId.New(),
            Name = name,
            Price = price,
            CategoryId = categoryId,
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id));

        return product;
    }

    public void UpdateInfo(ProductName name, CategoryId categoryId, string? description)
    {
        Name = name;
        CategoryId = categoryId;
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(Money newPrice)
    {
        if (Price == newPrice) return; // Không raise event nếu giá không đổi

        var oldPrice = Price;
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
