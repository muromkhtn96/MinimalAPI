using Microsoft.Extensions.Options;
using MinimalAPI.Application.Abstractions.Messaging;

namespace MinimalAPI.Infrastructure.Messaging.Kafka;

/// <summary>
/// Nối cấu hình topic (appsettings → Kafka:Topics) với các class integration event trong Application.
///
/// Mô hình: MỘT topic chứa NHIỀU loại event của cùng bounded context.
///   "minimalapi.orders": ["OrderCreatedIntegrationEvent", "OrderUpdatedIntegrationEvent"]
///   - Producer: từ kiểu event suy ra topic đích (GetTopicFor).
///   - Consumer: đọc header "event-type" của message để biết deserialize thành kiểu nào
///     (TryGetEventTypeByName) — vì topic không còn xác định duy nhất một kiểu event.
///
/// Validate NGAY khi khởi động (fail-fast): config gõ sai tên class, hoặc một event
/// bị gán vào 2 topic → app không start, báo lỗi rõ ràng thay vì chạy êm rồi mất message.
///
/// Thêm event mới chỉ cần 2 bước: tạo class {Entity}{Action}IntegrationEvent (Application)
/// + thêm tên class vào danh sách của topic trong appsettings — KHÔNG phải sửa file này.
/// </summary>
public sealed class KafkaTopicResolver
{
    private readonly IReadOnlyDictionary<Type, string> _eventTypeToTopic;
    private readonly IReadOnlyDictionary<string, Type> _eventNameToType;

    /// <summary> Toàn bộ topic đã khai báo — consumer subscribe danh sách này. </summary>
    public IReadOnlyCollection<string> AllTopics { get; }

    public KafkaTopicResolver(IOptions<KafkaOptions> options)
    {
        // Bước 1: tìm mọi integration event trong assembly Application
        var eventTypesByName = typeof(IntegrationEvent).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(IntegrationEvent)) && !type.IsAbstract)
            .ToDictionary(type => type.Name);

        // Bước 2: nối config với class — fail-fast nếu config sai
        var eventTypeToTopic = new Dictionary<Type, string>();
        var eventNameToType = new Dictionary<string, Type>();
        var unknownNames = new List<string>();

        foreach (var (topicName, eventClassNames) in options.Value.Topics)
        {
            foreach (var eventClassName in eventClassNames)
            {
                if (!eventTypesByName.TryGetValue(eventClassName, out var eventType))
                {
                    unknownNames.Add(eventClassName);
                    continue;
                }

                if (eventTypeToTopic.TryGetValue(eventType, out var existingTopic))
                    throw new InvalidOperationException(
                        $"Event {eventClassName} bị khai báo ở 2 topic: '{existingTopic}' và '{topicName}'. " +
                        "Mỗi event chỉ thuộc đúng MỘT topic.");

                eventTypeToTopic[eventType] = topicName;
                eventNameToType[eventClassName] = eventType;
            }
        }

        if (unknownNames.Count > 0)
            throw new InvalidOperationException(
                $"Kafka:Topics khai báo event không tồn tại: {string.Join(", ", unknownNames)}. " +
                $"Tên phải trùng TÊN CLASS integration event. Các event hợp lệ: {string.Join(", ", eventTypesByName.Keys)}.");

        _eventTypeToTopic = eventTypeToTopic;
        _eventNameToType = eventNameToType;
        AllTopics = options.Value.Topics.Keys.ToList();
    }

    /// <summary> Producer dùng: lấy topic đích cho một event. Chưa khai báo → báo lỗi rõ ràng. </summary>
    public string GetTopicFor(Type eventType)
    {
        if (_eventTypeToTopic.TryGetValue(eventType, out var topic))
            return topic;

        throw new InvalidOperationException(
            $"Chưa khai báo topic cho event {eventType.Name} — thêm \"{eventType.Name}\" vào danh sách event " +
            "của topic tương ứng trong section Kafka:Topics (appsettings.json).");
    }

    /// <summary> Consumer dùng: từ header "event-type" của message suy ra kiểu event để deserialize. </summary>
    public bool TryGetEventTypeByName(string eventTypeName, out Type eventType) =>
        _eventNameToType.TryGetValue(eventTypeName, out eventType!);
}
