namespace MinimalAPI.Domain.Primitives;

/// <summary>
/// Marker interface cho Domain Event — sự kiện xảy ra trong domain.
/// Không phụ thuộc MediatR — Infrastructure layer sẽ dispatch.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
