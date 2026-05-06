using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của IProductRepository.</summary>
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default) =>
        await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <inheritdoc />
    public void Add(Product product) => db.Products.Add(product);

    /// <inheritdoc />
    public void Remove(Product product) => db.Products.Remove(product);

    /// <inheritdoc />
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default)
    {
        return await db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveAsync(ProductId id, CancellationToken ct = default)
    {
        return await db.Products
            .Where(p => p.Id == id)
            .Select(p => p.IsActive)
            .FirstOrDefaultAsync(ct);
    }

}
