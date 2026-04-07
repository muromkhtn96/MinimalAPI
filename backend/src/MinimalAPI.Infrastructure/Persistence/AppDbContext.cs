using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Infrastructure.Persistence;

/// <summary>
/// Wrapper để bridge IDomainEvent (Domain) → INotification (MediatR).
/// Domain không phụ thuộc MediatR, Infrastructure chịu trách nhiệm dispatch.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;

/// <summary>
/// EF Core DbContext — quản lý kết nối DB và dispatch domain events sau khi save.
/// Implement IApplicationDbContext để Application layer query bằng LINQ.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    // IApplicationDbContext — expose IQueryable cho query handlers
    IQueryable<Product> IApplicationDbContext.Products => Products.AsNoTracking();
    IQueryable<Category> IApplicationDbContext.Categories => Categories.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Lưu thay đổi trước, lấy domain events sau
        var result = await base.SaveChangesAsync(cancellationToken);

        // Thu thập domain events từ tất cả aggregate roots (generic scan)
        var aggregates = ChangeTracker.Entries<IHasDomainEvents>().ToList();

        var domainEvents = aggregates
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Xóa events khỏi entities
        foreach (var entry in aggregates)
            entry.Entity.ClearDomainEvents();

        // Dispatch events qua MediatR — wrap IDomainEvent trong INotification
        foreach (var domainEvent in domainEvents)
            await mediator.Publish(new DomainEventNotification(domainEvent), cancellationToken);

        return result;
    }
}
