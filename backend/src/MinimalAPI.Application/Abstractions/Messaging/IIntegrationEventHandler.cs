namespace MinimalAPI.Application.Abstractions.Messaging;

/// <summary>
/// Contract xử lý integration event phía consumer.
/// KafkaConsumerService (Infrastructure) resolve handler theo kiểu event và gọi HandleAsync.
/// LƯU Ý: consumer chạy at-least-once — handler phải viết idempotent
/// (một event có thể được giao lại lần nữa nếu app chết trước khi commit offset).
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
