using MinimalAPI.Application.Abstractions.Messaging;

namespace MinimalAPI.Application.IntegrationEvents.Orders;

/// <summary>
/// Integration event phát lên Kafka khi đơn hàng mới được tạo thành công (sau khi commit DB).
/// Topic: minimalapi.orders (khai báo trong appsettings — Kafka:Topics) — chung topic với OrderUpdated,
/// consumer phân biệt qua header "event-type".
/// Payload phẳng, tự chứa — consumer không cần truy vấn DB để hiểu event.
/// </summary>
public sealed record OrderCreatedIntegrationEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<OrderEventItem> Items { get; init; }

    // Cùng OrderId → cùng partition → các event của một đơn hàng giữ đúng thứ tự
    public override string PartitionKey => OrderId.ToString();
}
