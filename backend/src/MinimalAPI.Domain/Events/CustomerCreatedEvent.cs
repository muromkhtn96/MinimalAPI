using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Events;

public sealed record CustomerCreatedEvent(CustomerId CustomerId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}