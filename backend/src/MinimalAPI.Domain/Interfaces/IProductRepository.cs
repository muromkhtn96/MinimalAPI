using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Product.</summary>
public interface IProductRepository : IRepository<Product, ProductId>
{
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    Task<List<Product>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);

    /// <summary>Lấy danh sách sản phẩm theo danh mục.</summary>
    Task<List<Product>> GetByCategoryAsync(CategoryId categoryId, CancellationToken ct = default);

    /// <summary>Lấy danh sách sản phẩm đang hoạt động.</summary>
    Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default);

    /// <summary>Lấy danh sách sản phẩm tắt hoạt động.</summary>
    Task<List<Product>> GetDeactiveProductsAsync(CancellationToken ct = default);

    /// <summary>Kiểm tra tên sản phẩm đã tồn tại chưa.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}
