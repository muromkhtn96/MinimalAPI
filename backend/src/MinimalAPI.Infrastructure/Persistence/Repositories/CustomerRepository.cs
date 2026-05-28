using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Infrastructure.Persistence.Repositories;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;
public sealed class CustomerRepository(AppDbContext db)
    : Repository<Customer, CustomerId>(db), ICustomerRepository
{
    /// <inheritdoc/>
    public async Task<List<Customer>> GetAllAsync(string? keywords, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var term = keywords.ToLower().Trim();
            query = query.Where(c => 
                (c.FullName != null && EF.Functions.ILike(c.FullName, term)) || 
                (c.Phone != null && EF.Functions.ILike(c.Phone, term)) || 
                EF.Functions.ILike(c.Code, term));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
    /// <inheritdoc/>
    public async Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default)
    {
        return await Set.AnyAsync(x => x.Phone == phone, ct);
    }
    /// <inheritdoc/>
    public async Task<Customer?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(x => x.Code == code, ct);
    }
}