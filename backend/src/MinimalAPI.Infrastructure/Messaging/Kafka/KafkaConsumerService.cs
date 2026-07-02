using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinimalAPI.Application.Abstractions.Messaging;

namespace MinimalAPI.Infrastructure.Messaging.Kafka;

/// <summary>
/// Chạy TẤT CẢ consumer group khai báo trong Kafka:Consumers (appsettings).
///
/// Mô hình (giống Kafka chuẩn production):
///   - Producer bắn vào MỘT topic (ví dụ minimalapi.orders).
///   - NHIỀU consumer group cùng subscribe topic đó — mỗi group một mục đích
///     (send-email, sync-report, notify-customer...), một offset riêng,
///     nhận ĐỦ mọi message độc lập với các group khác.
///   - Trên Kafka UI: tab Consumers của topic hiện đủ các GroupId.
///
/// Mỗi group chạy một consume loop trên thread riêng. Message đến → đọc header "event-type"
/// → deserialize → chỉ gọi những handler THUỘC GROUP đó (khai báo trong config).
///
/// Lifetime: đăng ký bằng AddHostedService — framework giữ đúng MỘT instance chạy nền
/// suốt vòng đời app (bản chất là Singleton). Handler thì Scoped — mỗi message một DI scope.
///
/// Độ tin cậy: at-least-once — chỉ commit offset SAU khi mọi handler của group chạy xong.
/// Handler lỗi → Seek về đúng offset đó để message ĐƯỢC GIAO LẠI (không lướt qua),
/// retry sau một khoảng nghỉ. Đổi lại handler phải idempotent.
/// </summary>
public sealed class KafkaConsumerService : BackgroundService
{
    // Handler lỗi → nghỉ bao lâu trước khi thử lại message đó (tránh retry dồn dập)
    private static readonly TimeSpan HandlerRetryDelay = TimeSpan.FromSeconds(5);

    // Broker lỗi tạm thời → nghỉ bao lâu trước khi Consume tiếp (tránh hot-spin đốt CPU + spam log)
    private static readonly TimeSpan ConsumeErrorDelay = TimeSpan.FromSeconds(1);

    /// <summary> Kế hoạch chạy của một group sau khi validate: đọc topic nào, event nào giao cho handler nào. </summary>
    private sealed record ConsumerGroupPlan(
        string GroupId,
        IReadOnlyList<string> Topics,
        IReadOnlyDictionary<Type, IReadOnlyList<Type>> HandlerTypesByEventType);

    private readonly KafkaOptions _options;
    private readonly KafkaTopicResolver _topicResolver;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IReadOnlyList<ConsumerGroupPlan> _groupPlans;

    // Cache MethodInfo của HandleAsync theo kiểu event — tránh reflection lookup lặp lại mỗi message.
    // ConcurrentDictionary vì nhiều consume loop (nhiều group) cùng ghi.
    private readonly ConcurrentDictionary<Type, MethodInfo> _handleMethodCache = [];

    public KafkaConsumerService(
        IOptions<KafkaOptions> options,
        KafkaTopicResolver topicResolver,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumerService> logger)
    {
        _options = options.Value;
        _topicResolver = topicResolver;
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Validate config NGAY khi khởi động (fail-fast) — sai tên topic/handler thì app không start
        _groupPlans = BuildGroupPlans(_options, topicResolver);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ConsumerEnabled)
        {
            _logger.LogInformation("[Kafka Consumer] Đang TẮT theo cấu hình (Kafka:ConsumerEnabled = false)");
            return Task.CompletedTask;
        }

        if (_groupPlans.Count == 0)
        {
            _logger.LogWarning("[Kafka Consumer] Không có consumer group nào trong Kafka:Consumers — consumer không khởi động");
            return Task.CompletedTask;
        }

        // Fail-fast nốt phần DI: handler có trong config nhưng quên đăng ký DI
        // → phát hiện NGAY khi start thay vì lặng lẽ bỏ qua message lúc runtime
        EnsureHandlersRegisteredInDi();

        // Mỗi group một consume loop độc lập trên thread riêng (Consume() là blocking call)
        var loops = _groupPlans
            .Select(plan => Task.Run(() => RunGroupLoopSafeAsync(plan, stoppingToken), stoppingToken))
            .ToList();

        return Task.WhenAll(loops);
    }

    /// <summary>
    /// Bọc ngoài cùng của một group: mọi exception không lường trước (Build/Subscribe lỗi,
    /// bug trong loop...) phải được log CRITICAL — nếu không, Task chết im lặng trong
    /// Task.WhenAll và group ngừng hoạt động mà không ai biết (app vẫn "healthy").
    /// </summary>
    private async Task RunGroupLoopSafeAsync(ConsumerGroupPlan group, CancellationToken stoppingToken)
    {
        try
        {
            await ConsumeLoopAsync(group, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // App đang shutdown — kết thúc bình thường
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[Kafka Consumer:{GroupId}] DỪNG HẲN do lỗi không phục hồi được — message của group này KHÔNG được xử lý nữa, cần restart app",
                group.GroupId);
        }
    }

    private async Task ConsumeLoopAsync(ConsumerGroupPlan group, CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId,
            GroupId = group.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest, // group mới đọc từ đầu topic
            EnableAutoCommit = false,                   // manual commit sau khi handler thành công
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError("[Kafka Consumer:{GroupId}] Lỗi kết nối: {Reason} (code {Code})",
                    group.GroupId, error.Reason, error.Code))
            .Build();

        consumer.Subscribe(group.Topics);
        _logger.LogInformation(
            "[Kafka Consumer:{GroupId}] Khởi động — subscribe: {Topics}, handlers: {Handlers}",
            group.GroupId,
            string.Join(", ", group.Topics),
            string.Join(", ", group.HandlerTypesByEventType.Values.SelectMany(h => h).Select(h => h.Name)));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // --- Bước 1: nhận message ---
                ConsumeResult<string, string>? consumeResult;
                try
                {
                    consumeResult = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    if (ex.Error.IsFatal)
                    {
                        // Lỗi không phục hồi được (vd: bị thu hồi quyền) — dừng group, log CRITICAL ở lớp ngoài
                        throw;
                    }

                    // Lỗi tạm thời (broker down...) — log rồi nghỉ 1 nhịp, KHÔNG hot-spin đốt CPU
                    _logger.LogError(ex, "[Kafka Consumer:{GroupId}] Lỗi nhận message: {Reason}",
                        group.GroupId, ex.Error.Reason);
                    await Task.Delay(ConsumeErrorDelay, stoppingToken);
                    continue;
                }

                if (consumeResult?.Message is null)
                    continue;

                // --- Bước 2: xử lý + commit ---
                try
                {
                    await DispatchToGroupHandlersAsync(group, consumeResult, stoppingToken);

                    // Chỉ commit khi mọi handler của group xử lý xong — đảm bảo at-least-once
                    consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException)
                {
                    throw; // app đang shutdown — không nuốt
                }
                catch (Exception ex)
                {
                    // Handler lỗi → PHẢI Seek về đúng offset này. Nếu chỉ "không commit" rồi đi tiếp,
                    // position trong memory vẫn tiến lên và lần commit thành công kế tiếp sẽ
                    // commit ĐÈ QUA message lỗi → mất message vĩnh viễn.
                    // Seek xong nghỉ 1 nhịp rồi thử lại — Phase 7 (roadmap): giới hạn số lần retry + Dead Letter Topic.
                    _logger.LogError(ex,
                        "[Kafka Consumer:{GroupId}] Xử lý message thất bại — topic {Topic} [partition {Partition}, offset {Offset}], sẽ thử lại sau {Delay}s",
                        group.GroupId, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value,
                        HandlerRetryDelay.TotalSeconds);

                    consumer.Seek(consumeResult.TopicPartitionOffset);
                    await Task.Delay(HandlerRetryDelay, stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close(); // rời group gọn gàng để rebalance ngay, không chờ session timeout
            _logger.LogInformation("[Kafka Consumer:{GroupId}] Đã dừng", group.GroupId);
        }
    }

    /// <summary>
    /// Đọc header "event-type" để biết message là event nào (một topic chứa nhiều loại event),
    /// deserialize rồi gọi những handler THUỘC GROUP này. Event không thuộc group → bỏ qua, vẫn commit.
    /// Một handler ném exception → propagate lên loop, offset không commit + Seek lại.
    /// </summary>
    private async Task DispatchToGroupHandlersAsync(
        ConsumerGroupPlan group,
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        var topic = consumeResult.Topic;

        // Tombstone / message rỗng (có thể xuất hiện khi compaction hoặc produce tay qua UI) — bỏ qua
        if (consumeResult.Message.Value is null)
        {
            _logger.LogWarning(
                "[Kafka Consumer:{GroupId}] Message không có nội dung (tombstone) — topic {Topic} [partition {Partition}, offset {Offset}], bỏ qua",
                group.GroupId, topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
            return;
        }

        // Header "event-type" do KafkaProducerService ghi — cho biết message là event nào trong topic
        if (!consumeResult.Message.Headers.TryGetLastBytes("event-type", out var eventTypeBytes))
        {
            _logger.LogWarning(
                "[Kafka Consumer:{GroupId}] Message thiếu header event-type — topic {Topic} [partition {Partition}, offset {Offset}], bỏ qua",
                group.GroupId, topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
            return;
        }

        var eventTypeName = Encoding.UTF8.GetString(eventTypeBytes);
        if (!_topicResolver.TryGetEventTypeByName(eventTypeName, out var eventType))
        {
            _logger.LogWarning(
                "[Kafka Consumer:{GroupId}] Event {EventTypeName} trên topic {Topic} chưa khai báo trong Kafka:Topics — bỏ qua",
                group.GroupId, eventTypeName, topic);
            return;
        }

        // Group này không đăng ký handler cho event này → bỏ qua, KHÔNG phải lỗi
        // (group nhận đủ mọi message của topic, chỉ xử lý event mình quan tâm)
        if (!group.HandlerTypesByEventType.TryGetValue(eventType, out var groupHandlerTypes))
            return;

        var integrationEvent = DeserializeOrNull(group.GroupId, consumeResult, eventType);
        if (integrationEvent is null)
            return; // message hỏng — đã log, commit để không chặn partition

        // Mỗi message một scope — handler dùng được các service Scoped (DbContext, repository...)
        await using var scope = _scopeFactory.CreateAsyncScope();

        // Resolve từ DI mọi handler của event, rồi CHỈ giữ những handler thuộc group này
        var handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.ServiceProvider.GetServices(handlerInterface)
            .Where(handler => groupHandlerTypes.Contains(handler!.GetType()))
            .ToList();

        var handleMethod = _handleMethodCache.GetOrAdd(eventType, type =>
            typeof(IIntegrationEventHandler<>).MakeGenericType(type)
                .GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync))!);

        foreach (var handler in handlers)
        {
            await (Task)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;

            _logger.LogInformation(
                "[Kafka Consumer:{GroupId}] {HandlerName} xử lý xong {EventType} (EventId {EventId}) — topic {Topic} [partition {Partition}, offset {Offset}]",
                group.GroupId, handler!.GetType().Name, eventType.Name, integrationEvent.EventId,
                topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
        }
    }

    /// <summary>
    /// Deserialize message; message hỏng (poison message) → log chi tiết rồi trả null
    /// để loop commit và đi tiếp, không chặn cả partition. Phase 7: đẩy sang Dead Letter Topic.
    /// </summary>
    private IntegrationEvent? DeserializeOrNull(string groupId, ConsumeResult<string, string> consumeResult, Type eventType)
    {
        try
        {
            var integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(
                consumeResult.Message.Value, eventType, JsonSerializerOptions.Web);

            if (integrationEvent is null)
                _logger.LogWarning("[Kafka Consumer:{GroupId}] Message null trên topic {Topic} — bỏ qua",
                    groupId, consumeResult.Topic);

            return integrationEvent;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "[Kafka Consumer:{GroupId}] Message sai định dạng JSON trên topic {Topic} [partition {Partition}, offset {Offset}] — bỏ qua để không chặn partition",
                groupId, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
            return null;
        }
    }

    /// <summary>
    /// Probe lúc khởi động: mọi handler trong group plan phải resolve được từ DI.
    /// Thiếu đăng ký DI mà chỉ cảnh báo lúc runtime = message bị commit và MẤT trong im lặng.
    /// (Application đã auto-scan đăng ký handler, probe này là chốt chặn cuối.)
    /// </summary>
    private void EnsureHandlersRegisteredInDi()
    {
        using var scope = _scopeFactory.CreateScope();

        foreach (var group in _groupPlans)
        foreach (var (eventType, handlerTypes) in group.HandlerTypesByEventType)
        {
            var handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            var registered = scope.ServiceProvider.GetServices(handlerInterface)
                .Select(handler => handler!.GetType())
                .ToHashSet();

            var missing = handlerTypes.Where(t => !registered.Contains(t)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"Consumer group {group.GroupId}: handler chưa được đăng ký DI: {string.Join(", ", missing.Select(t => t.Name))}. " +
                    "Kiểm tra auto-scan trong Application/DependencyInjection.cs.");
        }
    }

    /// <summary>
    /// Đọc Kafka:Consumers và validate fail-fast:
    ///   - GroupId không được trống/trùng nhau
    ///   - Topic của group phải khai báo trong Kafka:Topics
    ///   - Handler phải là class có thật implement IIntegrationEventHandler&lt;T&gt;
    ///     (một class được phép handle NHIỀU loại event — mọi event nó handle đều thuộc group)
    /// Sai ở đâu app không start và báo đúng chỗ đó — không chạy êm rồi lặng lẽ mất message.
    /// </summary>
    private static List<ConsumerGroupPlan> BuildGroupPlans(KafkaOptions options, KafkaTopicResolver topicResolver)
    {
        // Scan assembly Application: tên class handler → các cặp (kiểu handler, kiểu event nó xử lý).
        // GroupBy theo tên vì: (1) một class có thể implement handler cho nhiều event,
        // (2) hai class TRÙNG TÊN khác namespace là mơ hồ với config → phải báo lỗi rõ.
        var handlersByName = typeof(IntegrationEvent).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .Select(i => (HandlerType: type, EventType: i.GetGenericArguments()[0])))
            .GroupBy(entry => entry.HandlerType.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        var ambiguous = handlersByName
            .Where(pair => pair.Value.Select(e => e.HandlerType).Distinct().Count() > 1)
            .Select(pair => pair.Key)
            .ToList();
        if (ambiguous.Count > 0)
            throw new InvalidOperationException(
                $"Có nhiều class handler trùng tên (khác namespace): {string.Join(", ", ambiguous)}. " +
                "Kafka:Consumers tham chiếu theo tên class nên tên phải duy nhất — đổi tên một trong các class.");

        var plans = new List<ConsumerGroupPlan>();
        var seenGroupIds = new HashSet<string>();

        foreach (var consumer in options.Consumers)
        {
            if (string.IsNullOrWhiteSpace(consumer.GroupId))
                throw new InvalidOperationException("Kafka:Consumers có phần tử thiếu GroupId.");

            if (!seenGroupIds.Add(consumer.GroupId))
                throw new InvalidOperationException($"Kafka:Consumers khai báo trùng GroupId: {consumer.GroupId}.");

            var unknownTopics = consumer.Topics.Where(t => !topicResolver.AllTopics.Contains(t)).ToList();
            if (unknownTopics.Count > 0)
                throw new InvalidOperationException(
                    $"Consumer group {consumer.GroupId} subscribe topic chưa khai báo trong Kafka:Topics: {string.Join(", ", unknownTopics)}.");

            var handlersByEventType = new Dictionary<Type, List<Type>>();
            foreach (var handlerName in consumer.Handlers)
            {
                if (!handlersByName.TryGetValue(handlerName, out var handlerEntries))
                    throw new InvalidOperationException(
                        $"Consumer group {consumer.GroupId} khai báo handler không tồn tại: {handlerName}. " +
                        $"Handler hợp lệ: {string.Join(", ", handlersByName.Keys)}.");

                foreach (var (handlerType, eventType) in handlerEntries)
                {
                    if (!handlersByEventType.TryGetValue(eventType, out var list))
                        handlersByEventType[eventType] = list = [];
                    if (!list.Contains(handlerType))
                        list.Add(handlerType);
                }
            }

            plans.Add(new ConsumerGroupPlan(
                consumer.GroupId,
                consumer.Topics,
                handlersByEventType.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Type>)pair.Value)));
        }

        return plans;
    }
}
