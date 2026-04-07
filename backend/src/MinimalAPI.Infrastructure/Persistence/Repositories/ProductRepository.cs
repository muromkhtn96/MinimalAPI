using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của IProductRepository.</summary>
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <inheritdoc />
    public void Add(Product product) => db.Products.Add(product);

    /// <inheritdoc />
    public void Remove(Product product) => db.Products.Remove(product);
}
