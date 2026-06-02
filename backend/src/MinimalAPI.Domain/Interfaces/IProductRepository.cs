using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface IProductRepository : IRepository<Product, ProductId>
{
    /// <summary>
    /// Đếm số lượng sản phẩm theo tiêu chí tìm kiếm.
    /// </summary>
    /// <param name="search"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<int> CountAsync(string? search, CancellationToken ct = default);
    /// <summary>
    /// Lấy danh sách sản phẩm theo trang.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <param name="search"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<Product>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    /// <summary>
    /// Kiểm tra mã sản phẩm đã tồn tại chưa.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<Product>> GetByCategoryIdAsync(CategoryId categoryId, CancellationToken ct = default);
    /// <summary>
    /// Lấy danh sách sản phẩm đang hoạt động.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></ret
    Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Code"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Product?> GetByCodeAsync(string Code, CancellationToken ct);
    /// <summary>
    /// Lấy danh sách sản phẩm không hoạt động.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<Product>> GetDeactiveProductsAsync(CancellationToken ct = default);
    /// <summary>
    /// Kiểm tra tên sản phẩm đã tồn tại chưa.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}
