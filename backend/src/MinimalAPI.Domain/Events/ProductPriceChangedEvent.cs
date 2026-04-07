using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Domain.Events;

/// <summary>Sự kiện khi giá sản phẩm thay đổi.</summary>
public sealed record ProductPriceChangedEvent(
    ProductId ProductId,
    Money OldPrice,
    Money NewPrice) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
