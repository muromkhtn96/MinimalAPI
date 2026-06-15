using System;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Events;

public sealed record OrderConfirmedEvent(OrderId OrderId) : IDomainEvent
{
    /// <summary> Sự kiện được phát ra khi một đơn hàng được xác nhận. </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}