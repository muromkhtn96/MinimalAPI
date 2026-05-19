namespace MinimalAPI.Domain.Interfaces;

/// <summary>
/// Tạo mới một Unit of Work (đã mở transaction) cho mỗi thao tác ghi.
/// Dùng kèm <c>await using</c> để tự rollback nếu không commit.
/// </summary>
public interface IUnitOfWorkManager
{
    /// <summary>Mở transaction mới và trả về Unit of Work tương ứng.</summary>
    Task<IUnitOfWork> NewUnitOfWorkAsync(CancellationToken ct = default);
}
