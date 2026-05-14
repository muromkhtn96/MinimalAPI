using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository(AppDbContext db) : IInventoryRepository
{
    public async Task<List<Inventory>> GetAllAsync(CancellationToken ct = default) =>
    await db.Inventories
        .Include(i => i.Product)
        .OrderByDescending(i => i.CreatedAt)
        .ToListAsync(ct);
    public async Task<Inventory?> GetByIdAsync(InventoryId id, CancellationToken ct = default) =>
        await db.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    public async Task<bool> ExistsByProductIdAsync(ProductId productId, CancellationToken ct = default) =>
        await db.Inventories.AnyAsync(i => i.ProductId == productId, ct);

    public async Task<Inventory?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default) =>
        await db.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public void Add(Inventory inventory) => db.Inventories.Add(inventory);

    public void Remove(Inventory inventory) => db.Inventories.Remove(inventory);
}
