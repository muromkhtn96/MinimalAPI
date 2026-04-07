using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Events;

/// <summary>Sự kiện khi tạo sản phẩm mới.</summary>
public sealed record ProductCreatedEvent(ProductId ProductId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
