using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của IInventoryRepository.</summary>
public sealed class InventoryRepository(AppDbContext db)
    : Repository<Inventory, InventoryId>(db), IInventoryRepository
{
    /// <inheritdoc />
    public async Task<List<Inventory>> GetAllAsync(CancellationToken ct = default) =>
        await Set
            .Include(i => i.Product)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    /// <inheritdoc />
    public override async Task<Inventory?> GetByIdAsync(InventoryId id, CancellationToken ct = default) =>
        await Set
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByProductIdAsync(ProductId productId, CancellationToken ct = default) =>
        await Set.AnyAsync(i => i.ProductId == productId, ct);

    /// <inheritdoc />
    public async Task<Inventory?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default) =>
        await Set
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);
}
