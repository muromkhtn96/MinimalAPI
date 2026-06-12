using MinimalAPI.Domain.Primitives;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Domain.Entities;

/// <summary>
/// Đại diện cho một dòng chi tiết sản phẩm nằm bên trong Đơn hàng.
/// </summary>
public sealed class OrderDetail : Entity<OrderDetailId>
{
    /// <summary>
    /// Khởi tạo một đối tượng OrderDetail với ID đã cho.
    /// </summary>
    /// <param name="id"></param>
    public OrderDetail(OrderDetailId id) : base(id) { }

    /// <summary>
    /// Khởi tạo một đối tượng OrderDetail mới.
    /// </summary>
    public OrderDetail() { }

    /// <summary>
    /// Định danh đơn hàng mà dòng chi tiết này thuộc về.
    /// </summary>
    public OrderId OrderId { get; set; } = default!;
    /// <summary>
    /// Định danh sản phẩm.
    /// </summary>
    public ProductId ProductId { get; set; } = default!;
    /// <summary>
    /// Số lượng sản phẩm.
    /// </summary>
    public int Quantity { get; set; }
    /// <summary>
    /// Đơn giá của sản phẩm.
    /// </summary>
    public Money UnitPrice { get; set; } = default!;
    /// <summary>
    /// Tổng tiền của dòng chi tiết.
    /// </summary>
    public Money LineTotal => UnitPrice * Quantity;
}