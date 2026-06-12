using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Interfaces;

/// <summary>
/// Repository chung cho Aggregate Root — gom CRUD lặp lại
/// (insert / update / delete + lấy theo Id). Repo cụ thể kế thừa
/// và bổ sung query riêng của entity đó.
/// </summary>
public interface IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>Lấy entity theo Id (null nếu không tìm thấy).</summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);

    /// <summary>Thêm entity mới.</summary>
    void Add(TEntity entity);

    /// <summary>Đánh dấu entity là đã thay đổi.</summary>
    void Update(TEntity entity);

    /// <summary>Đánh dấu entity để xóa.</summary>
    void Remove(TEntity entity);
    /// <summary>
    /// Thêm nhiều entity vào kho lưu trữ.
    /// </summary>
    /// <param name="entities">Danh sách các entity cần thêm.</param>
    void AddRange(IEnumerable<TEntity> entities);
    /// <summary>
    ///  Cập nhật nhiều entity trong kho lưu trữ.
    /// </summary>
    /// <param name="entities">Danh sách các entity cần cập nhật.</param>
    void UpdateRange(IEnumerable<TEntity> entities);
    /// <summary>
    /// Xóa nhiều entity khỏi kho lưu trữ.
    /// </summary>
    /// <param name="entities">Danh sách các entity cần xóa.</param>
    void RemoveRange(IEnumerable<TEntity> entities);

}
