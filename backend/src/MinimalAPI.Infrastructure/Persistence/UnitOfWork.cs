using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
