namespace MinimalAPI.Domain.Interfaces;

/// <summary>Unit of Work — gom nhiều thay đổi và commit 1 lần (transaction).</summary>
public interface IUnitOfWork
{
    /// <summary>Khởi đầu một transaction mới.</summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Lưu tất cả thay đổi vào DB.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Hoàn tác tất cả thay đổi trong transaction hiện tại.</summary>
    Task RollbackAsync(CancellationToken ct = default);

    /// <summary>Commit transaction hiện tại.</summary>
    Task CommitAsync(CancellationToken ct = default);
}
