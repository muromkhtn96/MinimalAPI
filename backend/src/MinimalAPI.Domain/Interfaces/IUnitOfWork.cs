namespace MinimalAPI.Domain.Interfaces;

/// <summary>Unit of Work — gom nhiều thay đổi và commit 1 lần (transaction).</summary>
public interface IUnitOfWork
{
    /// <summary>Lưu tất cả thay đổi vào DB.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
