using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface IOrderDetailRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<OrderDetail?> GetByIdAsync(OrderDetailId id, CancellationToken ct = default);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    void Add(OrderDetail entity);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    void Update(OrderDetail entity);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    void Remove(OrderDetail entity);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<OrderDetail>> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default);
    /// <summary>
    /// Thêm nhiều chi tiết đơn hàng vào kho lưu trữ.
    /// </summary>
    /// <param name="entities">Danh sách các chi tiết đơn hàng cần thêm.</param>
    void AddRange(IEnumerable<OrderDetail> entities);
    /// <summary>
    /// Cập nhật nhiều chi tiết đơn hàng trong kho lưu trữ.
    /// </summary>
    /// <param name="entities">Danh sách các chi tiết đơn hàng cần cập nhật.</param>
    void RemoveRange(IEnumerable<OrderDetail> entities);
}