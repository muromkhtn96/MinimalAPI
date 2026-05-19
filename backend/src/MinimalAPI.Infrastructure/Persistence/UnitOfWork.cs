using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    /// <summary>
    /// Mở transaction mới. Không hỗ trợ transaction lồng nhau — nếu đã có transaction đang mở thì sẽ ném InvalidOperationException.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Đã có transaction đang mở — không hỗ trợ transaction lồng nhau.");

        _transaction = await db.Database.BeginTransactionAsync(ct);
    }

    /// <summary>
    /// Lưu thay đổi rồi commit transaction hiện tại. Nếu không có transaction nào đang mở thì vẫn sẽ lưu thay đổi bình thường.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            if (_transaction is not null)
                await _transaction.CommitAsync(ct);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Rollback transaction hiện tại. Nếu không có transaction nào đang mở thì vẫn sẽ chạy bình thường (không ném lỗi).
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        try
        {
            // Rollback phải chạy kể cả khi request bị hủy (ct đã cancelled)
            if (_transaction is not null)
                await _transaction.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Giải phóng transaction (gọi sau khi commit hoặc rollback xong).
    /// </summary>
    /// <returns></returns>
    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Safety-net cho <c>await using</c>: nếu transaction vẫn còn mở
    /// (quên commit / lỗi giữa chừng) thì rollback rồi giải phóng.
    /// Idempotent — sau commit/rollback _transaction đã null nên là no-op.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_transaction is not null)
                await _transaction.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }
}
