namespace MinimalAPI.Domain.Entities;

public readonly record struct InventoryId(Guid Value)
{
    public static InventoryId New() => new(Guid.NewGuid());
}