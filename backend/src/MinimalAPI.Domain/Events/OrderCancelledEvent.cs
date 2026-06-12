using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Events;

public sealed record OrderCancelledEvent(OrderId OrderId) : IDomainEvent
{
    /// <summary> Sự kiện được phát ra khi một đơn hàng bị hủy. </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}