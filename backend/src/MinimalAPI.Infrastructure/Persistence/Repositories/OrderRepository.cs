using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;
public sealed class OrderRepository(AppDbContext db)
    : Repository<Order, OrderId>(db), IOrderRepository
{
    /// <inheritdoc/>
    public async Task<Order?> GetByOrderIdAsync(OrderId id, CancellationToken ct = default)
{
    return await Set
        .Include(o => o.Details)
        .FirstOrDefaultAsync(o => o.Id == id, ct);
}
    /// <inheritdoc/>
    public async Task<List<Order>> GetPageAsync(int page, int pageSize, string? search, CancellationToken ct = default)
        {
            var query = Set.AsQueryable();

            return await query
            .Include(o => o.Details)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        }
    /// <inheritdoc/>
    public async Task<Order?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await Set
            .Include(o => o.Details)
            .FirstOrDefaultAsync(x => x.Code == code, ct);
    }
    public async Task<List<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken ct = default)
    {
        return await Set
            .Include(o => o.Details)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(ct);
    }
}