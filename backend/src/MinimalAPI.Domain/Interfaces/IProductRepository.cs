using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>Repository cho Aggregate Root — Product.</summary>
public interface IProductRepository
{
    /// <summary>Lấy sản phẩm theo Id (null nếu không tìm thấy).</summary>
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);

    /// <summary>Thêm sản phẩm mới vào DbContext.</summary>
    void Add(Product product);

    /// <summary>Đánh dấu sản phẩm để xóa.</summary>
    void Remove(Product product);

    /// <summary>Kiểm tra sản phẩm tắt hoạt động.</summary>
    Task<bool> IsDeactiveAsync(ProductId id, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetDeactiveProductsAsync(CancellationToken ct);
    
    /// <summary> Tắt hoạt động sản phẩm. </summary>
    Task DeactivateAsync(ProductId id, CancellationToken ct = default);
}
