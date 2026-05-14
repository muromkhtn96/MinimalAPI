using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByIdAsync(InventoryId id, CancellationToken ct = default);
    Task<List<Inventory>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByProductIdAsync(ProductId productId, CancellationToken ct = default);
    Task<Inventory?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default);
    void Add(Inventory inventory);
    void Remove(Inventory inventory);
}