using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Category.</summary>
public interface ICategoryRepository : IRepository<Category, CategoryId>
{
    Task<int> CountAsync(CancellationToken ct = default);
    Task<List<Category>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CategoryId excludeId, CancellationToken ct = default);
}
