using MinimalAPI.Domain.Exceptions;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

public sealed class Inventory : AggregateRoot<InventoryId>
{
    public ProductId ProductId { get; private set; }
    public Product Product { get; private set; } = default!;
    public long Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Inventory() { }

    public static Inventory Create(ProductId productId, long quantity)
    {
        EnsureQuantityIsValid(quantity);

        return new Inventory
        {
            Id = InventoryId.New(),
            ProductId = productId,
            Quantity = quantity,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateQuantity(long quantity)
    {
        EnsureQuantityIsValid(quantity);

        if (Quantity == quantity) return;

        Quantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureQuantityIsValid(long quantity)
    {
        if (quantity < 0)
            throw new DomainException("Inventory quantity cannot be negative.");
    }
}
