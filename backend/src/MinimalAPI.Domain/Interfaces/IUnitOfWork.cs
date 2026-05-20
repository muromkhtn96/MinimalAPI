namespace MinimalAPI.Domain.Interfaces;

/// <summary>
/// Unit of Work — gom nhiều thay đổi và commit 1 lần (transaction).
/// IAsyncDisposable: dùng với <c>await using</c> — tự rollback nếu chưa commit.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Lưu tất cả thay đổi vào DB.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Mở 1 transaction mới.</summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Lưu thay đổi rồi commit transaction hiện tại (gọi trong khối try).</summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>Rollback transaction hiện tại (gọi trong khối catch).</summary>
    Task RollbackAsync(CancellationToken ct = default);
}
