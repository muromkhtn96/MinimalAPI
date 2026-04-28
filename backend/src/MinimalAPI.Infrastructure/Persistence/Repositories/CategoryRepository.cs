using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của ICategoryRepository.</summary>
public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default) =>
        await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await db.Categories.CountAsync(ct);

    /// <inheritdoc />
    public async Task<List<Category>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default) =>
        await db.Categories
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        await db.Categories.AnyAsync(c => c.Name == name, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CategoryId excludeId, CancellationToken ct = default) =>
        await db.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId, ct);

    /// <inheritdoc />
    public void Add(Category category) => db.Categories.Add(category);

    /// <inheritdoc />
    public void Remove(Category category) => db.Categories.Remove(category);
}
