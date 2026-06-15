using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Events;

public sealed record OrderCreatedEvent(OrderId OrderId) : IDomainEvent
{
    /// <summary> Sự kiện được phát ra khi một đơn hàng mới được tạo. </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}