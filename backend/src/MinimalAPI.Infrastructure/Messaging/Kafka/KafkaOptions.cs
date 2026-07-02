namespace MinimalAPI.Infrastructure.Messaging.Kafka;

/// <summary>
/// Cấu hình Kafka — bind từ section "Kafka" trong appsettings.json.
/// </summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary> Danh sách broker, phân cách bằng dấu phẩy. Host dev: localhost:9092, trong compose: kafka:19092. </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary> Định danh client hiển thị trong log/metrics của broker. </summary>
    public string ClientId { get; set; } = "minimalapi";

    /// <summary> Cho phép tắt toàn bộ consumer nền (chạy test, migration, hoặc instance chỉ produce). </summary>
    public bool ConsumerEnabled { get; set; } = true;

    /// <summary>
    /// Mapping topic → danh sách event chứa trong topic đó.
    /// MỘT topic chứa NHIỀU loại event của cùng một bounded context — ví dụ:
    ///   "minimalapi.orders": ["OrderCreatedIntegrationEvent", "OrderUpdatedIntegrationEvent"]
    /// Nhờ vậy mọi event của cùng một đơn hàng (cùng PartitionKey) vào cùng partition
    /// → giữ đúng thứ tự created → updated. Consumer phân biệt loại event qua header "event-type".
    /// </summary>
    public Dictionary<string, string[]> Topics { get; set; } = [];

    /// <summary>
    /// Danh sách consumer group — MỘT topic có thể có NHIỀU group cùng đọc độc lập,
    /// mỗi group một mục đích, một offset riêng, nhận ĐỦ mọi message của topic
    /// (trên Kafka UI: tab Consumers của topic sẽ hiện đủ các GroupId này).
    /// </summary>
    public List<KafkaConsumerGroupOptions> Consumers { get; set; } = [];
}

/// <summary>
/// Cấu hình một consumer group: đọc topic nào và giao message cho những handler nào.
/// GroupId đặt theo convention {app}.{context}.{mục-đích}, ví dụ: minimalapi.orders.send-email.
/// </summary>
public sealed class KafkaConsumerGroupOptions
{
    public string GroupId { get; set; } = string.Empty;

    /// <summary> Các topic group này subscribe — phải khai báo trong Kafka:Topics. </summary>
    public string[] Topics { get; set; } = [];

    /// <summary>
    /// Tên class handler (implement IIntegrationEventHandler&lt;T&gt;) mà group này chạy.
    /// Event trong topic không thuộc handler nào của group → group bỏ qua (bình thường).
    /// </summary>
    public string[] Handlers { get; set; } = [];
}
