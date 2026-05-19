using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation của ICategoryRepository.</summary>
public sealed class CategoryRepository(AppDbContext db)
    : Repository<Category, CategoryId>(db), ICategoryRepository
{
    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await Set.CountAsync(ct);

    /// <inheritdoc />
    public async Task<List<Category>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default) =>
        await Set
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        await Set.AnyAsync(c => c.Name == name, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CategoryId excludeId, CancellationToken ct = default) =>
        await Set.AnyAsync(c => c.Name == name && c.Id != excludeId, ct);
}
