using MinimalAPI.Domain.Enums;
using MinimalAPI.Domain.Primitives;
using MinimalAPI.Domain.ValueObjects;
using MinimalAPI.Domain.Events;

namespace MinimalAPI.Domain.Entities;
/// <summary>
/// Đại diện cho thông tin Đơn hàng trong hệ thống.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    /// <summary>
    /// Khởi tạo một đối tượng Order với ID đã cho.
    /// </summary>
    /// <param name="id"></param>
    public Order(OrderId id) : base(id) { }

    /// <summary>
    /// Khởi tạo một đối tượng Order mới.
    /// </summary>
    public Order() { }

    /// <summary>
    /// Mã đơn hàng
    /// </summary>
    public string Code { get; set; } = default!;
    /// <summary>
    /// Định danh khách hàng đặt đơn hàng này.
    /// </summary>
    public CustomerId CustomerId { get; set; }
    /// <summary>
    /// Trạng thái đơn hàng.
    /// </summary>
    public OrderStatus Status { get; set; }
    /// <summary>
    /// Tổng số tiền của đơn hàng.
    /// </summary>
    public Money TotalAmount { get; set; } = Money.Zero;
    /// <summary>
    /// Ghi chú thêm về đơn hàng.
    /// </summary>
    public string? Note { get; set; }
    /// <summary>
    /// Ngày giờ tạo đơn hàng.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Ngày giờ cập nhật đơn hàng gần nhất.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Danh sách các dòng chi tiết sản phẩm nằm trong đơn hàng này.
    /// </summary>
    public List<OrderDetail> Details { get; set; } = [];

    /// <summary>
    /// Thêm sự kiện đơn hàng được tạo.
    /// </summary>
    public void AddCreatedEvent()
    {
        RaiseDomainEvent(new OrderCreatedEvent(Id));
    }

    /// <summary>
    /// Thêm sự kiện đơn hàng được xác nhận.
    /// </summary>
    public void AddConfirmedEvent()
    {
        RaiseDomainEvent(new OrderConfirmedEvent(Id));
    }

    /// <summary>
    /// Thêm sự kiện đơn hàng bị hủy.
    /// </summary>
    public void AddCancelledEvent()
    {
        RaiseDomainEvent(new OrderCancelledEvent(Id));
    }
}