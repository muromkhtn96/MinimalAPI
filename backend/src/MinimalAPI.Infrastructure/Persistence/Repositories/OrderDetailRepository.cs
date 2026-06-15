using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

public sealed class OrderDetailRepository(AppDbContext db) : IOrderDetailRepository
{
    /// <summary>
    /// Lấy DbSet của OrderDetail từ DbContext để thực hiện các thao tác truy vấn và cập nhật dữ liệu.
    /// </summary>
    private DbSet<OrderDetail> Set => db.Set<OrderDetail>();
    /// <summary>
    /// Lấy chi tiết đơn hàng theo ID.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<OrderDetail?> GetByIdAsync(OrderDetailId id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);
    /// <summary>
    /// Thêm một chi tiết đơn hàng mới vào kho lưu trữ.
    /// </summary>
    /// <param name="entity"></param>
    public void Add(OrderDetail entity) => Set.Add(entity);
    /// <summary>
    /// Cập nhật thông tin của một chi tiết đơn hàng.
    /// </summary>
    /// <param name="entity"></param>
    public void Update(OrderDetail entity) => Set.Update(entity);
    /// <summary>
    /// Xóa một chi tiết đơn hàng khỏi kho lưu trữ.
    /// </summary>
    /// <param name="entity"></param>
    public void Remove(OrderDetail entity) => Set.Remove(entity);
    /// <summary>
    /// Lấy danh sách chi tiết đơn hàng dựa trên ID của đơn hàng cha (OrderId).
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<List<OrderDetail>> GetByOrderIdAsync(OrderId orderId, CancellationToken ct = default)
    {
        return await Set
            .Where(d => d.OrderId == orderId)
            .ToListAsync(ct);
    }
    /// <summary>
    /// Thêm nhiều chi tiết đơn hàng vào kho lưu trữ.
    /// </summary>
    /// <param name="entities">Danh sách các chi tiết đơn hàng cần thêm.</param>
    public void AddRange(IEnumerable<OrderDetail> entities) => Set.AddRange(entities);
    /// <summary>
    /// Xóa nhiều chi tiết đơn hàng khỏi kho lưu trữ.
    /// </summary>
    /// <param name="entities">Danh sách các chi tiết đơn hàng cần xóa.</param>
    public void RemoveRange(IEnumerable<OrderDetail> entities) => Set.RemoveRange(entities);
}