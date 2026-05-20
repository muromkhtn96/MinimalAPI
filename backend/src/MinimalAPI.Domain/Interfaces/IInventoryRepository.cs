using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Inventory.</summary>
public interface IInventoryRepository : IRepository<Inventory, InventoryId>
{
    Task<List<Inventory>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByProductIdAsync(ProductId productId, CancellationToken ct = default);
    Task<Inventory?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default);
}
