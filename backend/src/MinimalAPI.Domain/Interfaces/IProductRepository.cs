using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Product.</summary>
public interface IProductRepository
{
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    Task<List<Product>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);

    /// <summary>Lấy sản phẩm theo Id (null nếu không tìm thấy).</summary>
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);

    /// <summary>Thêm sản phẩm mới vào DbContext.</summary>
    void Add(Product product);

    /// <summary>Đánh dấu sản phẩm để xóa.</summary>
    void Remove(Product product);
}
