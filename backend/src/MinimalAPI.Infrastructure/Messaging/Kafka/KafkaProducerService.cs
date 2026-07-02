using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinimalAPI.Application.Abstractions.Messaging;

namespace MinimalAPI.Infrastructure.Messaging.Kafka;

/// <summary>
/// Triển khai IIntegrationEventPublisher bằng Kafka (Confluent.Kafka).
///
/// Lifetime: SINGLETON — bắt buộc, vì:
///   - IProducer giữ TCP connection + buffer nội bộ, khởi tạo tốn kém (librdkafka).
///   - IProducer thread-safe, một instance phục vụ toàn app là thiết kế chuẩn của Confluent.
///   - Nếu đăng ký Scoped: mỗi request tạo connection mới → chậm và rò tài nguyên.
///
/// Độ tin cậy: Acks.All + EnableIdempotence → producer tự retry khi lỗi mạng
/// mà KHÔNG tạo message trùng trong partition.
/// </summary>
public sealed class KafkaProducerService : IIntegrationEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaTopicResolver _topicResolver;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(
        IOptions<KafkaOptions> options,
        KafkaTopicResolver topicResolver,
        ILogger<KafkaProducerService> logger)
    {
        _topicResolver = topicResolver;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = options.Value.ClientId,
            Acks = Acks.All,            // chờ toàn bộ in-sync replica xác nhận — không mất message khi broker chết
            EnableIdempotence = true,   // broker loại message trùng khi producer retry

            // QUAN TRỌNG: mặc định librdkafka là 300_000ms (5 phút) — broker down sẽ treo
            // HTTP request 5 phút trước khi ProduceAsync ném lỗi. Giới hạn 10s: publish nằm
            // sau commit DB, thà báo lỗi nhanh (đã có log + Phase 6 Outbox) còn hơn treo request.
            MessageTimeoutMs = 10_000,
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError("Kafka producer gặp lỗi kết nối: {Reason} (code {Code})", error.Reason, error.Code))
            .Build();
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        // Lấy kiểu runtime (không dùng typeof(TEvent)) — phòng trường hợp caller truyền qua biến kiểu base
        var eventType = integrationEvent.GetType();
        var topic = _topicResolver.GetTopicFor(eventType);

        var message = new Message<string, string>
        {
            // Key = PartitionKey: message cùng key vào cùng partition → giữ thứ tự cho cùng 1 aggregate.
            // JsonSerializerOptions.Web (built-in, camelCase) — consumer deserialize bằng đúng options này.
            Key = integrationEvent.PartitionKey,
            Value = JsonSerializer.Serialize(integrationEvent, eventType, JsonSerializerOptions.Web),
            Headers =
            [
                new Header("event-type", Encoding.UTF8.GetBytes(eventType.Name)),
                new Header("event-id", Encoding.UTF8.GetBytes(integrationEvent.EventId.ToString())),
            ],
        };

        var result = await _producer.ProduceAsync(topic, message, cancellationToken);

        _logger.LogInformation(
            "[Kafka Producer] Đã publish {EventType} (EventId {EventId}, Key {Key}) lên topic {Topic} [partition {Partition}, offset {Offset}]",
            eventType.Name, integrationEvent.EventId, message.Key, topic,
            result.Partition.Value, result.Offset.Value);
    }

    public void Dispose()
    {
        // Đẩy nốt message còn trong buffer trước khi app tắt — tối đa 5 giây
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
