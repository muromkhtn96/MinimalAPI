using Microsoft.EntityFrameworkCore;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base EF Core cho Aggregate Root — định nghĩa 1 lần insert/update/delete + GetById.
/// <see cref="GetByIdAsync"/> là virtual để repo con override khi cần Include navigation.
/// </summary>
public abstract class Repository<TEntity, TId>(AppDbContext db) : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    protected AppDbContext Db { get; } = db;
    protected DbSet<TEntity> Set => Db.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);

    public void Add(TEntity entity) => Set.Add(entity);

    public void Update(TEntity entity) => Set.Update(entity);

    public void Remove(TEntity entity) => Set.Remove(entity);

    public void AddRange(IEnumerable<TEntity> entities) => Set.AddRange(entities);
    
    public void UpdateRange(IEnumerable<TEntity> entities) => Set.UpdateRange(entities);

    public void RemoveRange(IEnumerable<TEntity> entities) => Set.RemoveRange(entities);
}
