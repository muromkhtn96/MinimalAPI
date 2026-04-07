namespace MinimalAPI.Domain.Primitives;

/// <summary>
/// Non-generic interface để scan domain events từ ChangeTracker
/// mà không cần biết TId cụ thể.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

/// <summary>
/// Base class cho Aggregate Root — entity gốc quản lý domain events.
/// Chỉ Aggregate Root mới có repository riêng.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Danh sách domain events chưa được dispatch.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>Thêm event vào danh sách — sẽ được dispatch khi SaveChanges.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>Xóa events sau khi đã dispatch xong.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
