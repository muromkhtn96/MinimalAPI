using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Category.</summary>
public interface ICategoryRepository
{
    /// <summary>Lấy danh mục theo Id (null nếu không tìm thấy).</summary>
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);
    Task<List<Category>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CategoryId excludeId, CancellationToken ct = default);
    void Add(Category category);
    void Remove(Category category);
}
