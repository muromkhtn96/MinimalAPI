namespace MinimalAPI.Application.Abstractions.Messaging;

/// <summary>
/// Base record cho mọi integration event — sự kiện publish ra ngoài process (Kafka).
/// Khác với IDomainEvent (nội bộ, MediatR): payload phải phẳng, tự chứa,
/// không dùng Typed ID/Value Object để consumer bên ngoài deserialize được.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary> Định danh duy nhất của event — consumer dùng làm idempotency key (chống xử lý trùng). </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary> Thời điểm event xảy ra (UTC). </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Key quyết định partition — message cùng key luôn vào cùng partition,
    /// Kafka đảm bảo thứ tự xử lý cho cùng một aggregate. Thường là Id của aggregate root.
    /// </summary>
    public abstract string PartitionKey { get; }
}
