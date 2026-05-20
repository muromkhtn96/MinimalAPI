using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence;

/// <summary>
/// Tạo Unit of Work mới (đã mở transaction) trên AppDbContext scoped hiện tại.
/// Mỗi thao tác ghi dùng 1 instance riêng, giải phóng qua <c>await using</c>.
/// </summary>
public sealed class UnitOfWorkManager(AppDbContext db) : IUnitOfWorkManager
{
    public async Task<IUnitOfWork> NewUnitOfWorkAsync(CancellationToken ct = default)
    {
        var unitOfWork = new UnitOfWork(db);
        await unitOfWork.BeginTransactionAsync(ct);
        return unitOfWork;
    }
}
