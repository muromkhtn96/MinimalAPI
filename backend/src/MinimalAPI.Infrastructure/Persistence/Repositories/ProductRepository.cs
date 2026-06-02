using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation của IProductRepository.
/// </summary>
public sealed class ProductRepository(AppDbContext db)
    : Repository<Product, ProductId>(db), IProductRepository
{
    public async Task<Product?> GetByCodeAsync(string Code, CancellationToken ct = default) =>
        await Set
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Code == Code, ct);

    /// <inheritdoc />
    public async Task<int> CountAsync(string? search, CancellationToken ct = default)
    {
        var query = Set.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower().Trim();
            var likeTerm = $"%{term}%"; 
            
            query = query.Where(c =>
                EF.Functions.ILike(c.Name.Value, likeTerm) ||
                (c.Code != null && EF.Functions.ILike(c.Code, likeTerm)) ||
                (c.Description != null && EF.Functions.ILike(c.Description, likeTerm)));
        }

        return await query.CountAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = Set.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower().Trim();
            var likeTerm = $"%{term}%";
            
            query = query.Where(c =>
                EF.Functions.ILike(c.Name.Value, likeTerm) || 
                (c.Code != null && EF.Functions.ILike(c.Code, likeTerm)) ||
                (c.Description != null && EF.Functions.ILike(c.Description, likeTerm)));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetByCategoryIdAsync(CategoryId categoryId, CancellationToken ct = default)
    {
        return await Set
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default)
    {
        return await Set
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetDeactiveProductsAsync(CancellationToken ct = default)
    {
        return await Set
            .Include(p => p.Category)
            .Where(p => !p.IsActive)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        return await Set.AnyAsync(p => p.Name.Value == name, ct);
    }
}