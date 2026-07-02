# Kafka Integration Guide — Producer & Consumer

## Mục tiêu

Hướng dẫn này mô tả **roadmap**, **thiết kế chi tiết**, **quy tắc đặt tên** và **cách sử dụng** Kafka Producer/Consumer trong MinimalAPI — theo đúng kiến trúc 4-layer DDD (Domain → Application → Infrastructure → Api).

> 🎯 **Nguyên tắc quan trọng nhất**: Application layer **không được biết Kafka tồn tại**. Application chỉ biết abstraction (`IIntegrationEventPublisher`, `IIntegrationEventHandler<T>`). Kafka là chi tiết hạ tầng — nằm trọn trong Infrastructure. Ngày mai đổi sang RabbitMQ/Azure Service Bus, Application **không đổi một dòng nào**.

---

# 1. Roadmap triển khai

| Phase | Nội dung | Trạng thái |
|-------|----------|-----------|
| **Phase 1** | Hạ tầng: Kafka (KRaft) + Kafka UI trong `docker-compose.dev.yml` | ✅ Done |
| **Phase 2** | Abstractions ở Application: `IntegrationEvent`, `IIntegrationEventPublisher`, `IIntegrationEventHandler<T>` | ✅ Done |
| **Phase 3** | Producer: `KafkaProducerService` — publish JSON vào topic theo bounded context, idempotent, acks=all | ✅ Done |
| **Phase 4** | Consumer: `KafkaConsumerService` — NHIỀU consumer group đọc cùng topic, at-least-once, manual commit | ✅ Done |
| **Phase 5** | Ví dụ end-to-end: topic `minimalapi.orders` (OrderCreated + OrderUpdated) → 3 consumer group | ✅ Done |
| **Phase 6** | Outbox Pattern — đảm bảo event không mất khi Kafka down (transactional outbox table) | 🔜 Future |
| **Phase 7** | Retry + Dead Letter Topic (DLT) — message lỗi không chặn partition | 🔜 Future |
| **Phase 8** | Schema Registry (Avro/JSON Schema) — versioning contract giữa các service | 🔜 Future |

---

# 2. Kiến trúc tổng quan

## 2.1. Mô hình: 1 topic → nhiều loại event → nhiều consumer group

Đây là mô hình Kafka chuẩn production (giống hệ thống TposAdmin: topic `TmtCo.TposAdmin.Events.AppTenant` có 3 consumer group `.ACT`, `.OTP`, `.OUT` cùng đọc):

```text
CreateOrderHandler ──┐
                     │ publish (qua IIntegrationEventPublisher)
UpdateOrderHandler ──┘
        │
        ▼
┌─────────────────────────────── Topic: minimalapi.orders ────────────────────────────────┐
│  Chứa NHIỀU loại event của bounded context Orders, phân biệt bằng header "event-type":  │
│  OrderCreatedIntegrationEvent, OrderUpdatedIntegrationEvent, ... (sau này thêm tiếp)    │
│  Key = OrderId → mọi event của CÙNG một đơn vào CÙNG partition → giữ đúng thứ tự        │
└──────┬───────────────────────────────┬───────────────────────────────┬──────────────────┘
       │                               │                               │
       ▼ (offset riêng)                ▼ (offset riêng)                ▼ (offset riêng)
┌──────────────────────┐   ┌──────────────────────┐   ┌──────────────────────────┐
│ Group:               │   │ Group:               │   │ Group:                   │
│ minimalapi.orders    │   │ minimalapi.orders    │   │ minimalapi.orders        │
│   .send-email        │   │   .sync-report       │   │   .notify-customer       │
│                      │   │                      │   │                          │
│ Quan tâm:            │   │ Quan tâm:            │   │ Quan tâm:                │
│  OrderCreated        │   │  OrderCreated        │   │  OrderUpdated            │
│ Handler:             │   │ Handler:             │   │ Handler:                 │
│  OrderCreated        │   │  OrderCreated        │   │  OrderUpdated            │
│   SendEmailHandler   │   │   SyncReportHandler  │   │   NotifyCustomerHandler  │
└──────────────────────┘   └──────────────────────┘   └──────────────────────────┘
```

**Tính chất quan trọng của consumer group:**

- Mỗi group nhận **ĐỦ mọi message** của topic — 3 group = message được xử lý 3 nơi độc lập.
- Mỗi group có **offset riêng**: group send-email chậm/chết **không ảnh hưởng** group sync-report.
- Trong CÙNG một group, mỗi message chỉ được **một instance** xử lý (scale ngang: chạy 2 pod API cùng group thì Kafka chia partition cho 2 pod).
- Group nhận message của event mình không quan tâm → **bỏ qua và commit** (bình thường, không phải lỗi).
- Trên **Kafka UI** → topic `minimalapi.orders` → tab **Consumers**: thấy đủ 3 GroupId.

## 2.2. Phân bổ theo layer

| Layer | Thành phần | Vai trò |
|-------|-----------|---------|
| **Domain** | `IDomainEvent` (đã có) | Sự kiện **nội bộ** trong process — không thay đổi |
| **Application** | `Abstractions/Messaging/*`, `IntegrationEvents/*` | Contract + integration event + handler. **Không tham chiếu Confluent.Kafka** |
| **Infrastructure** | `Messaging/Kafka/*` | Producer/Consumer thật sự, resolve topic/group từ config. Nơi **duy nhất** biết Kafka |
| **Api** | `appsettings.json` (section `Kafka`) | Broker, topics, consumer groups |

```text
MinimalAPI.Application/
├── Abstractions/Messaging/
│   ├── IntegrationEvent.cs               # base record: EventId, OccurredAt, PartitionKey
│   ├── IIntegrationEventPublisher.cs     # contract producer
│   └── IIntegrationEventHandler.cs       # contract consumer handler
└── IntegrationEvents/Orders/
    ├── OrderCreatedIntegrationEvent.cs
    ├── OrderUpdatedIntegrationEvent.cs
    ├── OrderCreatedSendEmailHandler.cs   # handler của group send-email
    ├── OrderCreatedSyncReportHandler.cs  # handler của group sync-report
    └── OrderUpdatedNotifyCustomerHandler.cs  # handler của group notify-customer

MinimalAPI.Infrastructure/Messaging/Kafka/
├── KafkaOptions.cs           # POCO bind từ Kafka section (broker, topics, consumers)
├── KafkaTopicResolver.cs     # nối config topic ↔ class event, validate fail-fast
├── KafkaJson.cs              # JsonSerializerOptions dùng chung producer/consumer
├── KafkaProducerService.cs   # IIntegrationEventPublisher — Singleton
└── KafkaConsumerService.cs   # BackgroundService — chạy N consume loop (N group)
```

## 2.3. Domain Event vs Integration Event — phân biệt rõ

| | Domain Event | Integration Event |
|---|---|---|
| Phạm vi | **Trong process** (MediatR) | **Giữa các process/service** (Kafka) |
| Ví dụ | `OrderCreatedEvent` (Domain/Events) | `OrderCreatedIntegrationEvent` (Application/IntegrationEvents) |
| Payload | Typed ID, tham chiếu entity | **Dữ liệu phẳng, tự chứa** (Guid, string, decimal) |
| Serialize | Không bao giờ | JSON (camelCase) |

> ⚠️ **Không bao giờ** publish trực tiếp Domain Event lên Kafka — luôn map sang Integration Event có schema phẳng, ổn định.

---

# 3. Lifetime chuẩn — AddSingleton hay AddScoped?

Câu trả lời KHÔNG phải là một — mỗi thành phần một lifetime khác nhau, và lý do quan trọng hơn đáp án:

| Thành phần | Lifetime | Tại sao |
|-----------|----------|---------|
| `KafkaProducerService` | **Singleton** | `IProducer` thread-safe, giữ TCP connection + buffer, khởi tạo tốn kém (librdkafka). Scoped = mỗi request mở connection mới → chậm, rò tài nguyên. Đây là khuyến nghị chính thức của Confluent. |
| `KafkaConsumerService` | **AddHostedService** | Framework giữ đúng MỘT instance chạy nền suốt vòng đời app (bản chất Singleton). Không bao giờ đăng ký consumer bằng Scoped/Transient. |
| `KafkaTopicResolver` | **Singleton** | Mapping event↔topic bất biến, build 1 lần khi khởi động, validate fail-fast. |
| `IIntegrationEventHandler<T>` | **Scoped** | Mỗi message được xử lý trong một DI scope riêng (giống một HTTP request) → handler inject được `DbContext`, repository, `ICacheService`... an toàn. |

> 💡 **Quy tắc nhớ nhanh**: cái gì giữ **connection** → Singleton; cái gì chạy **nền suốt đời app** → HostedService; cái gì xử lý **từng message/request** → Scoped.

---

# 4. Quy tắc đặt tên — BẮT BUỘC tuân thủ

## 4.1. Topic — theo bounded context, KHÔNG theo từng event

Convention: `{app}.{bounded-context}` — lowercase, phân cách bằng `.`

| Topic | Chứa các event |
|-------|---------------|
| `minimalapi.orders` | `OrderCreatedIntegrationEvent`, `OrderUpdatedIntegrationEvent`, (future: confirmed, cancelled) |
| `minimalapi.products` (future) | `ProductPriceChangedIntegrationEvent`, ... |

**Tại sao 1 topic cho cả context thay vì 1 topic mỗi event?**
- Mọi event của cùng một đơn hàng (cùng `PartitionKey` = OrderId) vào **cùng partition** → Kafka đảm bảo consumer đọc đúng thứ tự `created → updated → confirmed`. Nếu tách 2 topic thì thứ tự giữa created/updated **không được đảm bảo**.
- Consumer phân biệt loại event qua **header `event-type`** (producer tự ghi).

## 4.2. Consumer Group

Convention: `{app}.{context}.{mục-đích}` — mỗi group một mục đích duy nhất:

| GroupId | Mục đích | Handler |
|---------|----------|---------|
| `minimalapi.orders.send-email` | Gửi email xác nhận | `OrderCreatedSendEmailHandler` |
| `minimalapi.orders.sync-report` | Đồng bộ báo cáo | `OrderCreatedSyncReportHandler` |
| `minimalapi.orders.notify-customer` | Thông báo khách khi đơn đổi | `OrderUpdatedNotifyCustomerHandler` |

## 4.3. File & class

| Thành phần | Convention | Ví dụ |
|-----------|-----------|-------|
| Integration Event | `{Entity}{Action}IntegrationEvent` (sealed record) | `OrderUpdatedIntegrationEvent` |
| Consumer Handler | `{Entity}{Action}{MụcĐích}Handler` | `OrderCreatedSendEmailHandler` |
| Vị trí event + handler | `Application/IntegrationEvents/{Entity}/` | `IntegrationEvents/Orders/` |

---

# 5. Cấu hình — tất cả trong appsettings.json

```json
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "ClientId": "minimalapi",
  "ConsumerEnabled": true,
  "Topics": {
    "minimalapi.orders": [
      "OrderCreatedIntegrationEvent",
      "OrderUpdatedIntegrationEvent"
    ]
  },
  "Consumers": [
    {
      "GroupId": "minimalapi.orders.send-email",
      "Topics": [ "minimalapi.orders" ],
      "Handlers": [ "OrderCreatedSendEmailHandler" ]
    },
    {
      "GroupId": "minimalapi.orders.sync-report",
      "Topics": [ "minimalapi.orders" ],
      "Handlers": [ "OrderCreatedSyncReportHandler" ]
    },
    {
      "GroupId": "minimalapi.orders.notify-customer",
      "Topics": [ "minimalapi.orders" ],
      "Handlers": [ "OrderUpdatedNotifyCustomerHandler" ]
    }
  ]
}
```

| Key | Ý nghĩa |
|-----|---------|
| `Topics` | Topic → danh sách TÊN CLASS event chứa trong topic đó |
| `Consumers[].GroupId` | Tên consumer group (hiện trên Kafka UI) |
| `Consumers[].Topics` | Group subscribe topic nào (phải có trong `Topics`) |
| `Consumers[].Handlers` | TÊN CLASS handler group này chạy |
| `ConsumerEnabled` | `false` = instance chỉ produce (chạy test/migration) |

> 🛡️ **Fail-fast**: gõ sai tên event/handler/topic trong config → **app không start** và báo đúng chỗ sai kèm danh sách tên hợp lệ (`KafkaTopicResolver` + `KafkaConsumerService.BuildGroupPlans` + probe DI validate lúc khởi động). Sai config không bao giờ chạy êm rồi lặng lẽ mất message.

> 🏭 **Production**: `appsettings.Production.json` đang đặt `ConsumerEnabled: false` vì môi trường prod (docker-compose.yml) **chưa có Kafka broker**. Khi prod có broker: set env `Kafka__BootstrapServers=<broker>` và `Kafka__ConsumerEnabled=true`.

## Docker Compose (Development)

```bash
docker compose -f docker-compose.dev.yml up -d db redis kafka kafka-ui
```

| Service | Địa chỉ | Ghi chú |
|---------|---------|---------|
| `kafka` | `localhost:9092` | API chạy trên host kết nối vào đây |
| `kafka` (internal) | `kafka:19092` | API chạy trong compose network (env `Kafka__BootstrapServers=kafka:19092`) |
| `kafka-ui` | `http://localhost:8085` | Xem topic, message, consumer group |

---

# 6. Luồng xử lý chi tiết

## 6.1. Producer (publish)

```text
CreateOrderHandler / UpdateOrderHandler (Application)
   │  1. Nghiệp vụ trong UnitOfWork
   │  2. CommitAsync() ──► DB đã lưu chắc chắn
   │  3. eventPublisher.PublishAsync(event)   ◄── SAU commit, bọc try/catch
   ▼
KafkaProducerService (Infrastructure, Singleton)
   │  - KafkaTopicResolver: kiểu event → topic (minimalapi.orders)
   │  - Serialize JSON camelCase (JsonSerializerOptions.Web — consumer dùng đúng options này)
   │  - Key = PartitionKey (OrderId), Headers: event-type + event-id
   │  - Acks.All + EnableIdempotence → retry không tạo message trùng
   │  - MessageTimeoutMs = 10s (mặc định librdkafka là 5 PHÚT — broker down sẽ treo request 5 phút!)
   ▼
Topic minimalapi.orders
```

> ⚠️ **Tradeoff hiện tại (Phase 5)**: nếu Kafka down ngay sau khi DB commit, event bị mất (chỉ còn log error) — request vẫn trả thành công vì nghiệp vụ ĐÃ xong. Publish lỗi **không được** làm fail request. Phase 6 (Outbox) giải quyết triệt để.

## 6.2. Consumer (mỗi group một loop)

```text
KafkaConsumerService.ExecuteAsync
   │  Đọc Kafka:Consumers → mỗi group một consume loop trên thread riêng
   ▼
Loop của group X:
   1. Consume() message
      - Lỗi broker tạm thời → log + nghỉ 1s rồi thử lại (không hot-spin đốt CPU)
      - Lỗi FATAL → group dừng + log CRITICAL (không chết im lặng)
   2. Đọc header "event-type" → KafkaTopicResolver → kiểu event
   3. Group X không đăng ký handler cho event này? → bỏ qua + commit (bình thường)
   4. Deserialize JSON → IntegrationEvent (message hỏng/tombstone → log + bỏ qua, không chặn partition)
   5. Tạo DI scope → resolve handler của group → HandleAsync()
   6. Thành công → Commit offset (at-least-once)
      Lỗi      → Seek về ĐÚNG offset đó + nghỉ 5s rồi thử lại
```

> ⚠️ **Tại sao phải Seek chứ không chỉ "bỏ commit"?** `Consume()` vẫn tiến position trong memory
> dù không commit — nếu chỉ bỏ commit rồi đi tiếp, lần commit thành công kế tiếp sẽ commit ĐÈ QUA
> message lỗi → **mất message vĩnh viễn**. Seek kéo position quay lại để message thật sự được giao lại.

---

# 7. Ví dụ sử dụng end-to-end

## 7.1. Producer — publish trong handler (sau commit)

```csharp
public sealed class UpdateOrderHandler(
    ...,
    IIntegrationEventPublisher eventPublisher,   // ◄── abstraction, KHÔNG phải Kafka
    ILogger<UpdateOrderHandler> logger)
{
    public async Task<Result<OrderDto>> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        // ... nghiệp vụ trong UnitOfWork ...
        await unitOfWork.CommitAsync(ct);

        await PublishOrderUpdatedAsync(order, preparedData, ct);  // sau commit, try/catch bên trong
        return Result<OrderDto>.Success(...);
    }

    private async Task PublishOrderUpdatedAsync(Order order, UpdatePreparedData preparedData, CancellationToken ct)
    {
        try
        {
            await eventPublisher.PublishAsync(new OrderUpdatedIntegrationEvent
            {
                OrderId = order.Id.Value,
                Code = order.Code,
                // ... payload phẳng, tự chứa ...
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Publish OrderUpdatedIntegrationEvent thất bại cho đơn {OrderId} — đơn hàng vẫn cập nhật thành công", order.Id.Value);
        }
    }
}
```

## 7.2. Consumer — handler thuần nghiệp vụ, không biết Kafka

```csharp
public sealed class OrderUpdatedNotifyCustomerHandler(
    ILogger<OrderUpdatedNotifyCustomerHandler> logger)
    : IIntegrationEventHandler<OrderUpdatedIntegrationEvent>
{
    public Task HandleAsync(OrderUpdatedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        logger.LogInformation("[NotifyCustomer] Thông báo khách {CustomerId}: đơn {Code} đã cập nhật...",
            integrationEvent.CustomerId, integrationEvent.Code);
        return Task.CompletedTask;
    }
}
```

Đăng ký DI (Application/DependencyInjection.cs) + gán vào group (appsettings) — xong.

## 7.3. Chạy thử

```bash
# Cách 1 — MỘT lệnh chạy hết (DB + Redis + Kafka + Kafka UI + Seq + API + Frontend):
docker compose -f docker-compose.dev.yml up --build -d

# Cách 2 — hạ tầng trong Docker, API chạy local (debug/hot-reload):
docker compose -f docker-compose.dev.yml up -d db redis kafka kafka-ui seq
cd backend && dotnet run --project src/MinimalAPI.Api

# 3. Tạo đơn hàng
curl -X POST http://localhost:5000/api/orders -H "Content-Type: application/json" \
  -d '{ "customerId": "<GUID>", "note": "Test Kafka", "items": [{ "productId": "<GUID>", "quantity": 1 }] }'

# 4. Cập nhật đơn hàng
curl -X PUT http://localhost:5000/api/orders/<orderId> -H "Content-Type: application/json" \
  -d '{ "note": "Đổi số lượng", "details": [{ "productId": "<GUID>", "quantity": 3 }] }'
```

**Quan sát log API** (hoặc Seq `http://localhost:8081`):

```text
[INF] [Kafka Producer] Đã publish OrderCreatedIntegrationEvent ... lên topic minimalapi.orders [partition 0, offset 0]
[INF] [Kafka Consumer:minimalapi.orders.send-email] OrderCreatedSendEmailHandler xử lý xong OrderCreatedIntegrationEvent ...
[INF] [Kafka Consumer:minimalapi.orders.sync-report] OrderCreatedSyncReportHandler xử lý xong OrderCreatedIntegrationEvent ...
[INF] [Kafka Producer] Đã publish OrderUpdatedIntegrationEvent ... lên topic minimalapi.orders [partition 0, offset 1]
[INF] [Kafka Consumer:minimalapi.orders.notify-customer] OrderUpdatedNotifyCustomerHandler xử lý xong OrderUpdatedIntegrationEvent ...
```

**Kafka UI** (`http://localhost:8085`) → Topics → `minimalapi.orders`:
- Tab **Messages**: message JSON, key = OrderId, header `event-type`
- Tab **Consumers**: đủ 3 group `send-email`, `sync-report`, `notify-customer` (giống mô hình TposAdmin)

---

# 8. Checklist mở rộng

## 8.1. Thêm event mới (ví dụ `OrderConfirmedIntegrationEvent`)

1. **Application**: tạo `IntegrationEvents/Orders/OrderConfirmedIntegrationEvent.cs` — sealed record kế thừa `IntegrationEvent`, override `PartitionKey` = OrderId. Item dùng chung `OrderEventItem`.
2. **appsettings**: thêm `"OrderConfirmedIntegrationEvent"` vào danh sách event của topic `minimalapi.orders`.
3. **Producer**: inject `IIntegrationEventPublisher` vào `ConfirmOrderHandler`, gọi `eventPublisher.PublishAfterCommitAsync(event, logger)` **sau** `CommitAsync` — extension này đã gói sẵn chính sách "lỗi publish chỉ log, không fail request, không dùng CancellationToken của request".
4. **Consumer** (nếu cần): tạo handler `OrderConfirmed{MụcĐích}Handler`, thêm tên vào `Handlers` của group phù hợp (hoặc tạo group mới). **KHÔNG cần đăng ký DI** — Application auto-scan toàn bộ `IIntegrationEventHandler<T>` trong assembly.

KHÔNG phải sửa `KafkaProducerService` / `KafkaConsumerService` / `KafkaTopicResolver`.

## 8.2. Thêm consumer group mới (mục đích mới trên topic có sẵn)

1. Tạo handler mới trong `Application/IntegrationEvents/{Entity}/` (DI tự scan).
2. Thêm một entry vào `Kafka:Consumers` với GroupId theo convention `{app}.{context}.{mục-đích}`.

Restart app — group mới tự đọc từ đầu topic (`AutoOffsetReset.Earliest`).

---

# 9. Lưu ý vận hành

- **Idempotency phía consumer**: at-least-once nghĩa là handler CÓ THỂ nhận 1 message 2 lần. Handler phải idempotent (check `EventId` đã xử lý chưa, hoặc thao tác upsert tự nhiên idempotent). Khách không được nhận 2 email!
- **Không xử lý nặng trong handler**: trong 1 group, 1 partition xử lý tuần tự — handler chậm sẽ dồn lag. Việc nặng → đẩy sang job riêng.
- **Schema evolution**: chỉ **thêm** field mới (có default), không đổi tên/xóa field — consumer cũ vẫn deserialize được.
- **Giám sát lag**: Kafka UI → Consumer Groups — lag của group nào tăng liên tục nghĩa là group đó không theo kịp producer (các group khác không bị ảnh hưởng).
- **Scale ngang**: chạy nhiều instance API cùng config → các instance cùng GroupId tự chia partition. Muốn tận dụng, topic cần nhiều partition (mặc định auto-create là 1).
