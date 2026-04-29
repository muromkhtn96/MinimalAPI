using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của IProductRepository.</summary>
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    /// <inheritdoc />
    public async Task<int> CountAsync(string? search, CancellationToken ct = default) =>
        await db.Products
            .Where(p => string.IsNullOrWhiteSpace(search) || p.Name.Value.ToLower().Contains(search.ToLower()))
            .CountAsync(ct);

    /// <inheritdoc />
    public async Task<List<Product>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default) =>
        await db.Products
            .Include(p => p.Category)
            .Where(p => string.IsNullOrWhiteSpace(search) || p.Name.Value.ToLower().Contains(search.ToLower()))
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default) =>
        await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<Product>> GetByCategoryAsync(CategoryId categoryId, CancellationToken ct = default) =>
        await db.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default) =>
    await db.Products
        .Include(p => p.Category)
        .Where(p => p.IsActive)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<List<Product>> GetDeactiveProductsAsync(CancellationToken ct = default) =>
        await db.Products
            .Include(p => p.Category)
            .Where(p => !p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    /// <inheritdoc />
    public void Add(Product product) => db.Products.Add(product);

    /// <inheritdoc />
    public void Remove(Product product) => db.Products.Remove(product);
}
