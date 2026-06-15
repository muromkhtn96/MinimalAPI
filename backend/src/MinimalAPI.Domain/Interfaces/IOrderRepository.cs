using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order, OrderId>
{
    /// <summary>
    /// Lấy đơn hàng theo ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Order?> GetByOrderIdAsync(OrderId id, CancellationToken ct = default);
    /// <summary>
    /// Lấy danh sách đơn hàng với phân trang và tìm kiếm
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<Order>> GetPageAsync(int Page, int PageSize, string? Search, CancellationToken ct = default);
    /// <summary>
    /// Lấy đơn hàng theo mã
    /// </summary>
    /// <param name="code"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Order?> GetByCodeAsync(string code, CancellationToken ct = default);
    /// <summary>
    /// Lấy danh sách đơn hàng của một khách hàng dựa trên ID khách hàng
    /// </summary>
    /// <param name="customerId"></param>
    Task<List<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken ct = default);
}