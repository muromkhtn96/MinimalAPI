namespace MinimalAPI.Application.Abstractions.Messaging;

/// <summary>
/// Abstraction publish integration event ra message broker.
/// Application chỉ phụ thuộc interface này — Infrastructure quyết định broker cụ thể (Kafka).
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publish một integration event. Topic đích được resolve từ kiểu event
    /// (mapping tập trung ở Infrastructure — KafkaTopics).
    /// </summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
