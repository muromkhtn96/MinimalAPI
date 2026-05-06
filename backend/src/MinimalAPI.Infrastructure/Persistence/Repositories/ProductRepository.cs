using Microsoft.EntityFrameworkCore;
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
    public async Task<bool> IsDeactiveAsync(ProductId id, CancellationToken ct = default) =>
        await db.Products.AnyAsync(p => p.Id == id && !p.IsActive, ct);
    
    public async Task<IEnumerable<Product>> GetDeactiveProductsAsync(CancellationToken ct) =>
        await db.Products
            .Include(p => p.Category)
            .Where(p => !p.IsActive)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task DeactivateAsync(ProductId id, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync(new object[] { id }, ct);
        if (product is not null)
        {
            product.Deactivate();
            await db.SaveChangesAsync(ct);
        }
    }
}
