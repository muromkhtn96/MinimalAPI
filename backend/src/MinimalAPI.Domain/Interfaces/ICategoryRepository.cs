using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Category.</summary>
public interface ICategoryRepository
{
    /// <summary>Lấy danh mục theo Id (null nếu không tìm thấy).</summary>
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);

    /// <summary>Kiểm tra tên danh mục đã tồn tại chưa.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Kiểm tra tên danh mục đã tồn tại chưa (trừ danh mục đang sửa).</summary>
    Task<bool> ExistsByNameAsync(string name, CategoryId excludeId, CancellationToken ct = default);

    /// <summary>Thêm danh mục mới vào DbContext.</summary>
    void Add(Category category);

    /// <summary>Đánh dấu danh mục để xóa.</summary>
    void Remove(Category category);
}
