# Order Feature Guide

## Mục tiêu

Feature Order dùng để quản lý **đơn hàng** trong hệ thống — gồm thông tin đơn (`Order`) và danh sách dòng hàng (`OrderDetail`). Đây là feature đầu tiên có **quan hệ cha–con trong cùng một Aggregate**, nên đọc kỹ phần [Aggregate & quan hệ](#aggregate--quan-hệ) trước khi code.

### Chức năng

- Tạo đơn hàng mới (kèm nhiều dòng hàng cùng lúc)
- Xem chi tiết đơn hàng theo Id (kèm các dòng hàng)
- Xem danh sách đơn hàng (paged, filter theo khách hàng / trạng thái)
- Tra cứu đơn theo mã (`DH00001`)
- Cập nhật trạng thái đơn (xác nhận / hủy)
- Hủy đơn hàng

> 🎯 **Bài học DDD quan trọng nhất của feature này**: `Order` là **Aggregate Root**, `OrderDetail` là **child entity** nằm bên trong. Chỉ `Order` mới có repository. Bạn **không** tạo `IOrderDetailRepository` — mọi thao tác với dòng hàng đều đi qua `Order`. Xem [Aggregate & quan hệ](#aggregate--quan-hệ).

---

# Thiết kế Database

## Order

### Mục đích

Lưu thông tin chung của một đơn hàng: mã đơn, khách hàng, trạng thái, tổng tiền, thời điểm tạo.

### Bảng `orders`

| Column          | Type          | Nullable | Description                                          |
|-----------------|---------------|----------|------------------------------------------------------|
| id              | uuid          | NO       | Khóa chính (`OrderId` — Typed ID)                    |
| code            | varchar(20)   | NO       | Mã đơn hàng (unique, VD `DH00001`) — sinh tự động    |
| customer_id     | uuid          | NO       | FK → `customers.id` (`CustomerId` — Typed ID)        |
| status          | smallint      | NO       | Trạng thái đơn (0: Pending, 1: Confirmed, 2: Cancelled) |
| total_amount    | decimal(18,2) | NO       | Tổng tiền — thành phần VO `Money`                    |
| total_currency  | varchar(3)    | NO       | Mã tiền tệ (VD `VND`) — VO `Money`                   |
| note            | text          | YES      | Ghi chú đơn hàng                                     |
| created_at      | timestamptz   | NO       | Ngày tạo (UTC)                                       |
| updated_at      | timestamptz   | YES      | Ngày cập nhật gần nhất (UTC)                         |

### Index đề xuất

| Index                      | Columns        | Loại    | Mục đích                          |
|----------------------------|----------------|---------|-----------------------------------|
| `ux_orders_code`           | `code`         | UNIQUE  | Tra cứu nhanh theo mã, đảm bảo unique |
| `ix_orders_customer_id`    | `customer_id`  | BTREE   | Lọc / join đơn theo khách hàng    |
| `ix_orders_status`         | `status`       | BTREE   | Lọc đơn theo trạng thái           |

## OrderDetail

### Mục đích

Lưu **từng dòng hàng** trong đơn: sản phẩm nào, số lượng bao nhiêu, đơn giá tại thời điểm đặt và thành tiền của dòng.

> ⚠️ **Lưu đơn giá tại thời điểm đặt** (`unit_price_*`) thay vì luôn lấy giá hiện tại của Product. Vì giá sản phẩm có thể đổi sau này (xem `ProductPriceChangedEvent`), nhưng đơn hàng cũ phải giữ nguyên giá đã chốt.

### Bảng `order_details`

| Column              | Type          | Nullable | Description                                       |
|---------------------|---------------|----------|---------------------------------------------------|
| id                  | uuid          | NO       | Khóa chính (`OrderDetailId` — Typed ID)           |
| order_id            | uuid          | NO       | FK → `orders.id` (`OrderId` — Typed ID)           |
| product_id          | uuid          | NO       | FK → `products.id` (`ProductId` — Typed ID)       |
| quantity            | int           | NO       | Số lượng đặt (> 0)                                |
| unit_price_amount   | decimal(18,2) | NO       | Đơn giá tại thời điểm đặt — VO `Money`            |
| unit_price_currency | varchar(3)    | NO       | Mã tiền tệ — VO `Money`                           |

> Thành tiền dòng (`line total`) = `unit_price * quantity` — **không lưu cột riêng**, tính bằng property `LineTotal` trong domain (tránh dữ liệu sai lệch).

### Index đề xuất

| Index                          | Columns        | Loại  | Mục đích                            |
|--------------------------------|----------------|-------|-------------------------------------|
| `ix_order_details_order_id`    | `order_id`     | BTREE | Lấy nhanh các dòng theo đơn         |
| `ix_order_details_product_id`  | `product_id`   | BTREE | Thống kê sản phẩm bán ra            |

> FK `order_id → orders.id` cấu hình `OnDelete: Cascade` — xóa Order thì các OrderDetail bị xóa theo. Đây là quan hệ **composition** (dòng hàng không tồn tại độc lập ngoài đơn).

---

# Aggregate & quan hệ

```text
(Customer) 1 ── * (Order)
(Order)    1 ── * (OrderDetail)        ← cha–con TRONG cùng aggregate
(Product)  1 ── * (OrderDetail)        ← chỉ tham chiếu qua ProductId
```

### Ranh giới Aggregate

```text
┌─────────── Aggregate "Order" ───────────┐
│  Order (Aggregate Root)                  │
│    └── List<OrderDetail>  (child)        │
└──────────────────────────────────────────┘
   ↑ chỉ Order có repository (IOrderRepository)
   ↑ OrderDetail KHÔNG có repository riêng
```

### Quy tắc vàng

```text
1. Mọi truy cập / thay đổi OrderDetail PHẢI đi qua Order.
   → order.AddItem(...), order.RemoveItem(...) — KHÔNG sửa trực tiếp từ ngoài.

2. Chỉ load Order là đủ — EF tự kéo theo OrderDetails (Include).
   → repo.GetByIdAsync(orderId) trả về Order kèm danh sách dòng.

3. Customer & Product nằm ở aggregate KHÁC.
   → Order chỉ giữ CustomerId / ProductId (Typed ID), không giữ object.
   → Muốn biết tên KH / SP thì join ở tầng Query (IApplicationDbContext).

4. TotalAmount được tính lại từ các dòng, không cho set tay từ ngoài.
```

---

# Cấu trúc Domain (DDD)

```
backend/src/MinimalAPI.Domain/
├── Entities/
│   ├── Order.cs                    # AggregateRoot<OrderId>, sealed, factory Create()
│   ├── OrderId.cs                  # readonly record struct OrderId(Guid Value)
│   ├── OrderDetail.cs              # Entity<OrderDetailId> (child — KHÔNG phải AggregateRoot)
│   └── OrderDetailId.cs            # readonly record struct OrderDetailId(Guid Value)
├── Enums/
│   └── OrderStatus.cs              # Pending = 0, Confirmed = 1, Cancelled = 2
├── Events/
│   ├── OrderCreatedEvent.cs        # raise khi tạo đơn
│   └── OrderCancelledEvent.cs      # raise khi hủy đơn
└── Interfaces/
    └── IOrderRepository.cs         # chỉ cho Aggregate Root Order
```

## Quy tắc Domain

- `Order` là **Aggregate Root**, kế thừa `AggregateRoot<OrderId>`, `sealed`.
- `OrderDetail` là **child entity**, kế thừa `Entity<OrderDetailId>` (có `Id` nhưng **không** kế thừa `AggregateRoot`, **không** có repository).
- KHÔNG public setter — thay đổi qua method (`Create`, `AddItem`, `RemoveItem`, `Confirm`, `Cancel`).
- `Money` là **Value Object** dùng cho `UnitPrice` (mỗi dòng) và `TotalAmount` (đơn).
- `OrderId`, `OrderDetailId`, `CustomerId`, `ProductId` là **Typed ID**.
- `Order.Items` để lộ ra ngoài dưới dạng `IReadOnlyList<OrderDetail>` (không cho thêm/xóa trực tiếp từ ngoài).
- `Order.TotalAmount` được **tính lại** mỗi khi thêm/bớt dòng — không nhận giá trị từ client.
- Raise `OrderCreatedEvent` khi tạo, `OrderCancelledEvent` khi hủy.

### `OrderDetail.cs` — child entity (ví dụ rút gọn)

```csharp
public sealed class OrderDetail : Entity<OrderDetailId>
{
    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    /// <summary>Thành tiền dòng = đơn giá × số lượng (tính, không lưu DB).</summary>
    public Money LineTotal => UnitPrice * Quantity;

    private OrderDetail() { } // EF Core

    // internal: chỉ Order (cùng assembly Domain) mới được tạo dòng hàng
    internal static OrderDetail Create(ProductId productId, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Số lượng dòng hàng phải lớn hơn 0.");

        return new OrderDetail
        {
            Id = OrderDetailId.New(),
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
```

> 💡 `Create` của `OrderDetail` để `internal` — chặn việc tạo dòng hàng tách rời khỏi `Order`. Ngoài aggregate chỉ gọi `order.AddItem(...)`.

### `Order.cs` — aggregate root (ví dụ rút gọn)

```csharp
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderDetail> _items = [];

    public string Code { get; private set; } = default!;
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero;
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Danh sách dòng hàng — chỉ đọc từ bên ngoài.</summary>
    public IReadOnlyList<OrderDetail> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(string code, CustomerId customerId, string? note)
    {
        var order = new Order
        {
            Id = OrderId.New(),
            Code = code,
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Note = note?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id));
        return order;
    }

    /// <summary>Thêm một dòng hàng vào đơn rồi tính lại tổng tiền.</summary>
    public void AddItem(ProductId productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Chỉ được thêm dòng khi đơn đang ở trạng thái chờ.");

        // Cùng sản phẩm thì cộng dồn số lượng thay vì tạo dòng mới
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
            existing.IncreaseQuantity(quantity);
        else
            _items.Add(OrderDetail.Create(productId, quantity, unitPrice));

        RecalculateTotal();
    }

    public void Confirm()
    {
        if (_items.Count == 0)
            throw new DomainException("Không thể xác nhận đơn rỗng.");
        if (Status != OrderStatus.Pending) return;

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled) return;

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderCancelledEvent(Id));
    }

    private void RecalculateTotal() =>
        TotalAmount = _items.Aggregate(Money.Zero, (sum, item) => sum + item.LineTotal);
}
```

> `RecalculateTotal()` cộng các `LineTotal` bằng toán tử `+` của `Money` (đã có sẵn, ép cùng currency). Vì khởi điểm là `Money.Zero` (VND), nếu trộn nhiều currency sẽ ném `DomainException` — phù hợp với giả định "một đơn dùng một loại tiền".

---

# Thiết kế API

## Endpoints

| Method | Route                          | Description                          |
|--------|--------------------------------|--------------------------------------|
| POST   | `/api/orders`                  | Tạo đơn hàng (kèm danh sách dòng)    |
| GET    | `/api/orders`                  | Danh sách (paged, filter)            |
| GET    | `/api/orders/{id}`             | Chi tiết đơn (kèm dòng hàng)         |
| GET    | `/api/orders/code/{code}`      | Tra cứu theo mã                      |
| PUT    | `/api/orders/{id}/confirm`     | Xác nhận đơn                         |
| PUT    | `/api/orders/{id}/cancel`      | Hủy đơn                              |

### Ví dụ — Tạo đơn hàng

```http
POST /api/orders
```

```json
{
  "customerId": "9c1f...",
  "note": "Giao giờ hành chính",
  "items": [
    { "productId": "8b1e...", "quantity": 2 },
    { "productId": "7a2d...", "quantity": 1 }
  ]
}
```

> Client **chỉ gửi `productId` + `quantity`**. Đơn giá (`unitPrice`) do server tự lấy từ Product tại thời điểm tạo — **không** tin giá client gửi lên.

```json
{
  "isSuccess": true,
  "value": {
    "id": "f4d2...",
    "code": "DH00001",
    "customerId": "9c1f...",
    "customerName": "Nguyễn Văn A",
    "status": "Pending",
    "totalAmount": 597000,
    "currency": "VND",
    "note": "Giao giờ hành chính",
    "items": [
      {
        "productId": "8b1e...",
        "productName": "Áo thun cotton",
        "quantity": 2,
        "unitPrice": 199000,
        "lineTotal": 398000
      },
      {
        "productId": "7a2d...",
        "productName": "Quần jean",
        "quantity": 1,
        "unitPrice": 199000,
        "lineTotal": 199000
      }
    ],
    "createdAt": "2026-06-02T00:00:00Z"
  }
}
```

## Vertical Slices (Application Layer)

```
backend/src/MinimalAPI.Application/Features/Orders/
├── CreateOrder/
│   ├── CreateOrderCommand.cs        # CustomerId, Note, List<OrderItemInput> Items
│   ├── CreateOrderHandler.cs
│   └── CreateOrderValidator.cs
├── ConfirmOrder/
│   ├── ConfirmOrderCommand.cs
│   └── ConfirmOrderHandler.cs
├── CancelOrder/
│   ├── CancelOrderCommand.cs
│   └── CancelOrderHandler.cs
├── GetOrder/
│   ├── GetOrderQuery.cs
│   └── GetOrderHandler.cs
├── GetOrders/
│   ├── GetOrdersQuery.cs
│   └── GetOrdersHandler.cs
├── GetOrderByCode/
│   ├── GetOrderByCodeQuery.cs
│   └── GetOrderByCodeHandler.cs
└── DTOs/
    ├── OrderDto.cs                  # Id, Code, CustomerId, CustomerName, Status, TotalAmount, Currency, Note, List<OrderItemDto> Items, CreatedAt
    └── OrderItemDto.cs              # ProductId, ProductName, Quantity, UnitPrice, LineTotal
```

Hạ tầng & API:

```
backend/src/MinimalAPI.Infrastructure/Persistence/
├── Configurations/OrderConfiguration.cs        # ToTable("orders"), ComplexProperty Money, HasMany OrderDetails
└── Repositories/OrderRepository.cs

backend/src/MinimalAPI.Api/Endpoints/OrderEndpoints.cs   # route group "/api/orders" + WithTags("Orders")
```

## Command tạo đơn (gợi ý)

```csharp
public record OrderItemInput(Guid ProductId, int Quantity);

public record CreateOrderCommand(
    Guid CustomerId,
    string? Note,
    List<OrderItemInput> Items) : IRequest<Result<OrderDto>>;
```

## CreateOrderHandler — luồng xử lý

Handler **không** chứa business logic, chỉ điều phối: kiểm tra tồn tại → gọi domain → lưu. Các bước:

```csharp
public sealed class CreateOrderHandler(
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo,
    IOrderRepository orderRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ICodeGenerator codeGenerator,
    ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1) Khách hàng phải tồn tại
        var customer = await customerRepo.GetByIdAsync(new CustomerId(request.CustomerId), ct);
        if (customer is null)
            return Result<OrderDto>.Failure("Khách hàng không tồn tại.");

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            // 2) Sinh mã đơn: DH00001, DH00002, ...
            var code = await codeGenerator.NextAsync("DH", 5, ct);
            var order = Order.Create(code, customer.Id, request.Note);

            // 3) Thêm từng dòng — lấy đơn giá từ Product, đồng thời TRỪ KHO
            foreach (var item in request.Items)
            {
                var product = await productRepo.GetByIdAsync(new ProductId(item.ProductId), ct);
                if (product is null)
                    return Result<OrderDto>.Failure($"Sản phẩm {item.ProductId} không tồn tại.");
                if (!product.IsActive)
                    return Result<OrderDto>.Failure($"Sản phẩm '{product.Name.Value}' đã ngừng bán.");

                // 3a) Tồn kho phải có và đủ số lượng
                var inventory = await inventoryRepo.GetByProductIdAsync(product.Id, ct);
                if (inventory is null)
                    return Result<OrderDto>.Failure($"Sản phẩm '{product.Name.Value}' chưa có tồn kho.");
                if (inventory.Quantity < item.Quantity)
                    return Result<OrderDto>.Failure(
                        $"Sản phẩm '{product.Name.Value}' không đủ tồn (còn {inventory.Quantity}, cần {item.Quantity}).");

                // 3b) Trừ kho — UpdateQuantity nhận SỐ LƯỢNG MỚI (tuyệt đối), không phải delta
                inventory.UpdateQuantity(inventory.Quantity - item.Quantity);
                inventoryRepo.Update(inventory);

                // 3c) Thêm dòng hàng (đơn giá lấy từ server, KHÔNG tin client)
                order.AddItem(product.Id, item.Quantity, product.Price);
            }

            // 4) Lưu — EF commit orders + order_details + inventories trong 1 transaction
            orderRepo.Add(order);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Đơn hàng {OrderId} ({Code}) đã tạo cho khách {CustomerId}",
                order.Id.Value, order.Code, customer.Id.Value);

            return Result<OrderDto>.Success(MapToDto(order, customer));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tạo đơn hàng thất bại - đã rollback");
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

> **Tồn kho (Inventory)** — điểm cần nhớ:
>
> - `Inventory` là Aggregate Root **riêng** (có `IInventoryRepository`), nên trừ kho = load → `UpdateQuantity` → `Update`, **nằm chung transaction** với việc tạo đơn. Nếu trừ kho hỏng giữa chừng thì `CommitAsync` không chạy, mọi thứ rollback — đơn không bị tạo "treo" còn kho thì đã trừ.
> - `UpdateQuantity(long)` nhận **số lượng mới tuyệt đối**, không phải lượng cần trừ → phải truyền `inventory.Quantity - item.Quantity`.
> - Kiểm tra `inventory.Quantity < item.Quantity` **trước** để trả `Result.Failure` thân thiện. Nếu để domain tự bắt (số âm) thì nó ném `DomainException` — không phải cách báo lỗi nghiệp vụ ta muốn.
> - **Hoàn kho khi hủy**: vì đã trừ kho lúc tạo (`Pending`), khi `Cancel` phải **cộng trả** số lượng về kho (xem [CancelOrderHandler](#cancelorderhandler--hoàn-kho)). Trừ ở đâu thì hoàn ở đó — giữ kho luôn cân.

## CancelOrderHandler — hoàn kho

Hủy đơn không chỉ đổi `Status` → còn phải **trả lại số lượng đã trừ** về kho, trong cùng một transaction.

```csharp
public sealed class CancelOrderHandler(
    IOrderRepository orderRepo,
    IInventoryRepository inventoryRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<CancelOrderHandler> logger)
    : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        // Load đơn KÈM Items — cần dòng hàng để biết hoàn bao nhiêu
        var order = await orderRepo.GetByIdWithItemsAsync(new OrderId(request.OrderId), ct);
        if (order is null)
            return Result<OrderDto>.Failure("Đơn hàng không tồn tại.");
        if (order.Status == OrderStatus.Cancelled)
            return Result<OrderDto>.Failure("Đơn hàng đã bị hủy trước đó.");

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            // Cộng trả tồn kho cho từng dòng
            foreach (var item in order.Items)
            {
                var inventory = await inventoryRepo.GetByProductIdAsync(item.ProductId, ct);
                if (inventory is not null)
                {
                    inventory.UpdateQuantity(inventory.Quantity + item.Quantity);
                    inventoryRepo.Update(inventory);
                }
            }

            order.Cancel();            // đổi Status + raise OrderCancelledEvent
            orderRepo.Update(order);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Đơn hàng {OrderId} ({Code}) đã hủy, đã hoàn kho",
                order.Id.Value, order.Code);

            return Result<OrderDto>.Success(MapToDto(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hủy đơn {OrderId} thất bại - đã rollback", request.OrderId);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

> ⚠️ Chỉ hoàn kho cho đơn đã thực sự trừ kho (ở đây mọi đơn `Pending`/`Confirmed` đều đã trừ lúc tạo). Nếu sau này đổi thiết kế sang "trừ kho khi Confirm", phải sửa lại logic hoàn kho cho khớp — nếu không sẽ **cộng dư** kho cho đơn chưa từng trừ.

## Repository (`IOrderRepository`)

Kế thừa `IRepository<Order, OrderId>`, bổ sung:

| Method                  | Mục đích                                            |
|-------------------------|------------------------------------------------------|
| `GetByIdWithItemsAsync` | Lấy đơn **kèm** `Items` (dùng `Include`)             |
| `GetByCodeAsync`        | Tra cứu đơn theo mã, kèm Items                       |
| `CountAsync`            | Đếm đơn (phục vụ phân trang, filter)                 |
| `GetPagedAsync`         | Lấy trang đơn theo `customerId` / `status`           |

> ⚠️ Vì OrderDetail là child, mặc định EF **không** load `Items`. Method lấy đơn để **sửa/hiển thị chi tiết** phải `.Include(o => o.Items)`, nếu không `order.Items` sẽ rỗng.

```csharp
public async Task<Order?> GetByIdWithItemsAsync(OrderId id, CancellationToken ct = default) =>
    await Set
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == id, ct);
```

---

# EF Core Configuration

Điểm khác biệt so với các entity trước: cấu hình **quan hệ cha–con** giữa `Order` và `OrderDetail`.

```csharp
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new OrderId(value));

        builder.Property(o => o.Code)
            .HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.HasIndex(o => o.Code).IsUnique();

        builder.Property(o => o.CustomerId)
            .HasColumnName("customer_id")
            .HasConversion(id => id.Value, value => new CustomerId(value));

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<short>();          // enum ↔ smallint

        // Money tổng tiền — ComplexProperty
        builder.ComplexProperty(o => o.TotalAmount, b =>
        {
            b.Property(m => m.Amount).HasColumnName("total_amount").HasColumnType("decimal(18,2)").IsRequired();
            b.Property(m => m.Currency).HasColumnName("total_currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(o => o.Note).HasColumnName("note");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");

        // ── Quan hệ cha–con: Order 1 — * OrderDetail ──
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF đọc field _items qua backing field
        builder.Metadata
            .FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.DomainEvents);
    }
}
```

`OrderDetail` cấu hình ở file riêng (`OrderDetailConfiguration`), nhưng **không** có repository:

```csharp
public sealed class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("order_details");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new OrderDetailId(value));

        builder.Property(d => d.OrderId)
            .HasColumnName("order_id")
            .HasConversion(id => id.Value, value => new OrderId(value));

        builder.Property(d => d.ProductId)
            .HasColumnName("product_id")
            .HasConversion(id => id.Value, value => new ProductId(value));

        builder.Property(d => d.Quantity).HasColumnName("quantity").IsRequired();

        builder.ComplexProperty(d => d.UnitPrice, b =>
        {
            b.Property(m => m.Amount).HasColumnName("unit_price_amount").HasColumnType("decimal(18,2)").IsRequired();
            b.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(3).IsRequired();
        });

        // LineTotal là property tính toán — KHÔNG map vào DB
        builder.Ignore(d => d.LineTotal);
    }
}
```

> Nhớ thêm `IQueryable<Order> Orders { get; }` vào `IApplicationDbContext` để các Query handler join lấy `CustomerName` / `ProductName`.

---

# Validation rules (FluentValidation)

| Field            | Rule                                                          |
|------------------|---------------------------------------------------------------|
| `CustomerId`     | Required (`NotEmpty`)                                         |
| `Items`          | Required, tối thiểu 1 dòng (`NotEmpty`)                        |
| `Items[].ProductId` | Required (`NotEmpty`)                                      |
| `Items[].Quantity`  | `> 0`                                                      |
| `Note`           | Optional                                                     |

```csharp
public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Khách hàng không được để trống.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một dòng.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Sản phẩm không được để trống.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
        });
    }
}
```

Kiểm tra nghiệp vụ bổ sung ở Handler:

| Rule                    | Mô tả                                                         |
|-------------------------|---------------------------------------------------------------|
| Khách hàng phải tồn tại | `ICustomerRepository.GetByIdAsync` trước khi tạo              |
| Sản phẩm phải tồn tại   | `IProductRepository.GetByIdAsync` cho từng dòng              |
| Sản phẩm phải đang bán  | Chặn nếu `product.IsActive == false`                         |
| Phải có tồn kho         | `IInventoryRepository.GetByProductIdAsync` — null thì Failure |
| Tồn kho phải đủ         | Chặn nếu `inventory.Quantity < item.Quantity`                |
| Đơn giá lấy từ server   | `unitPrice = product.Price` — không nhận giá từ client       |

---

# Business Rules

- `Order` là Aggregate Root; `OrderDetail` là child entity — chỉ thao tác qua `Order`, **không** có `IOrderDetailRepository`.
- Mã đơn `Code` (`DH00001`...) sinh tự động qua `ICodeGenerator.NextAsync("DH")`, unique, không cho client nhập.
- Đơn giá mỗi dòng (`UnitPrice`) chốt tại thời điểm đặt, lấy từ `Product.Price` của server.
- `TotalAmount` luôn được tính lại từ các dòng (`Σ LineTotal`) — không nhận từ client.
- Trạng thái đơn: `Pending → Confirmed` hoặc `Pending → Cancelled`. Không xác nhận đơn rỗng.
- Thêm dòng chỉ được phép khi đơn còn `Pending`.
- Cùng một sản phẩm trong đơn thì cộng dồn số lượng (tùy nghiệp vụ — có thể bỏ nếu muốn cho phép dòng trùng).
- Xóa Order thì các OrderDetail bị xóa theo (cascade).
- Tạo đơn raise `OrderCreatedEvent`; hủy đơn raise `OrderCancelledEvent`.
- Tạo đơn (sinh mã + trừ kho + insert order + insert details) nằm trong **một transaction** (`IUnitOfWorkManager`); hủy đơn (hoàn kho + đổi trạng thái) cũng vậy.
- Đã có tồn kho và đủ số lượng mới cho tạo đơn; trừ kho lúc tạo, hoàn kho lúc hủy.

---

# Câu hỏi cần làm rõ trước khi implement

1. **Thời điểm trừ kho**: guide này trừ ngay khi **tạo** (`Pending`). Có nghiệp vụ trừ khi `Confirm` (giữ chỗ tạm) — nếu đổi thì sửa cả logic hoàn kho.
2. **Dòng trùng sản phẩm**: cộng dồn số lượng hay cho phép nhiều dòng cùng sản phẩm?
3. **Sửa đơn**: có cho phép `PUT /api/orders/{id}` sửa lại dòng hàng sau khi tạo không, hay chỉ confirm/cancel? (Sửa dòng thì phải tính lại chênh lệch kho.)
4. **Giảm giá / phí ship**: tổng tiền có thêm chiết khấu, phí vận chuyển không? (Hiện chỉ `Σ LineTotal`.)
5. **Đa tiền tệ**: một đơn có thể trộn nhiều currency không? (Guide này giả định một đơn = một loại tiền.)
6. **Xóa đơn**: hard delete hay chuyển trạng thái `Cancelled` (soft)?

---

# Hướng mở rộng khi hệ thống lớn / nhiều đơn đồng thời

> 📌 Phần này **không cần làm ngay** — code ở trên đã chạy đúng cho tải vừa. Đây là bản đồ nâng cấp khi lượng đơn tăng và xuất hiện các vấn đề về **đồng thời (concurrency)** và **hiệu năng (performance)**. Đọc để biết "khi nào thì cần gì", đừng tối ưu sớm.

## 1. Vấn đề lõi: tranh chấp tồn kho (race condition)

Đây là vấn đề **nguy hiểm nhất** khi nhiều đơn mua cùng một sản phẩm tại cùng thời điểm:

```text
Kho còn 1 cái. T1 và T2 cùng vào:
T1: đọc Quantity = 1  →  thấy đủ  →  trừ về 0
T2: đọc Quantity = 1  →  thấy đủ  →  trừ về 0   ❌ bán 2 cái khi chỉ có 1 (oversell)
```

Code hiện tại (đọc rồi `UpdateQuantity`) **không** an toàn dưới tải cao. Các cách xử lý, từ nhẹ tới nặng:

| Cách | Cơ chế | Khi nào dùng | Đánh đổi |
|------|--------|--------------|----------|
| **Optimistic concurrency** | Thêm cột `version` (`xmin`/`RowVersion`) vào `inventories`; EF `IsConcurrencyToken()`. Đụng độ → `DbUpdateConcurrencyException` → retry. | Tải vừa, đụng độ ít | Phải code vòng retry; đụng nhiều thì retry liên tục |
| **Pessimistic lock (DB)** | `SELECT ... FOR UPDATE` (EF: `.FromSql` hoặc raw) khóa dòng kho tới hết transaction | Đụng độ nhiều, cần chắc chắn | Giảm song song, dễ deadlock nếu khóa nhiều dòng sai thứ tự |
| **Atomic UPDATE có điều kiện** | `UPDATE inventories SET quantity = quantity - @qty WHERE product_id = @id AND quantity >= @qty` — DB tự đảm bảo, 0 dòng affected = không đủ | Đơn giản, hiệu quả, khỏi đọc-rồi-ghi | Bỏ pattern domain `UpdateQuantity` cho path nóng |
| **Hàng đợi + Redis** | Xem mục 2 | Flash sale, cực nhiều đơn/giây | Phức tạp hạ tầng |

> 💡 Khuyến nghị theo thứ tự trưởng thành: bắt đầu **Atomic UPDATE có điều kiện** (rẻ, chặn oversell ngay) → thêm **optimistic version** nếu cần giữ pattern domain → chỉ dùng **Redis/queue** khi thật sự là hệ flash-sale.

## 2. Redis — khi nào và dùng làm gì

Redis **không** phải "thêm vào cho nhanh". Mỗi mục dưới đây giải một bài toán cụ thể:

- **Đếm tồn / giữ chỗ tồn kho (inventory reservation)**: giữ số lượng khả dụng trên Redis (`DECRBY` atomic). Đơn vào trừ Redis trước (nhanh, chặn oversell tức thì), DB cập nhật bất đồng bộ sau. Dùng cho **flash sale** — nơi Postgres không chịu nổi nghìn lượt/giây vào cùng một dòng.
- **Phân tán khóa (distributed lock)**: nhiều instance API → khóa theo `product_id` (RedLock) khi cần đoạn critical section xuyên service. Cân nhắc kỹ — lock phân tán khó làm đúng.
- **Idempotency key**: client gửi `Idempotency-Key` header; lưu key→kết quả trên Redis (TTL ngắn). Bấm "Đặt hàng" 2 lần / mạng retry → **không tạo 2 đơn**. Đây là thứ nên có sớm hơn người ta tưởng.
- **Cache đọc**: cache catalog sản phẩm/giá (đổi ít, đọc nhiều). **Không** cache số tồn kho realtime trừ khi chấp nhận sai số.
- **Rate limit**: chặn spam tạo đơn theo IP/user.

> ⚠️ Redis làm "nguồn sự thật" cho kho → phải có cơ chế **đồng bộ ngược về DB** và xử lý khi Redis chết (rebuild từ DB). Đây là phần khó nhất, đừng xem nhẹ.

## 3. Sinh mã đơn dưới tải cao

`ICodeGenerator.NextAsync("DH")` dùng atomic UPSERT Postgres — **an toàn concurrency** nhưng mỗi lần ghi một dòng counter, dễ thành điểm nóng (hot row) khi cực nhiều đơn/giây.

- Tải vừa: giữ nguyên, không cần đổi.
- Tải cao: cấp **dải số (batch/hi-lo)** — mỗi instance xin trước 1000 số, phát trong bộ nhớ, hết mới xin tiếp → giảm ghi DB 1000 lần. Đánh đổi: mã có thể **nhảy quãng** khi restart (chấp nhận được vì mã đơn không cần liên tục — khác hóa đơn pháp lý, xem [code-generator-guide.md](code-generator-guide.md)).

## 4. Hiệu năng đọc (N+1, projection)

- **Tránh N+1 khi load Items**: dùng `.Include(o => o.Items)` (đã nêu) thay vì lazy-load từng dòng. Khi list nhiều đơn kèm dòng, cân nhắc `AsSplitQuery()` để tránh nhân bản dữ liệu (cartesian explosion).
- **Query side dùng projection thẳng sang DTO**: handler đọc nên `IApplicationDbContext.Orders.Select(o => new OrderDto(...))` — chỉ kéo cột cần, không materialize cả entity. Tách hẳn model đọc/ghi (CQRS đúng nghĩa).
- **Phân trang bằng keyset** thay `Skip/Take` khi bảng đơn rất lớn (`Skip` chậm tuyến tính ở trang sâu).
- **Index**: bám đúng các index đã đề xuất (`customer_id`, `status`, `code`); thêm composite index theo cách lọc thực tế (VD `(customer_id, created_at desc)`).

## 5. Tách giao dịch nặng khỏi request (eventual consistency)

Khi nghiệp vụ sau khi đặt đơn phình to (gửi mail, đẩy kho ERP, ghi sổ kế toán, bắn thông báo):

- Đừng làm đồng bộ trong request tạo đơn → chậm và dễ vỡ transaction.
- Phát `OrderCreatedEvent` ra **message broker** (RabbitMQ/Kafka) hoặc **outbox pattern** (ghi event vào bảng `outbox` cùng transaction đơn, worker đọc và publish sau) → đảm bảo "tạo đơn xong thì event chắc chắn được xử lý, dù service phụ đang chết".
- Đây là bước chuyển từ monolith giao dịch sang kiến trúc hướng sự kiện — chỉ làm khi side-effect thật sự nhiều.

## 6. Lộ trình đề xuất (đừng làm hết một lúc)

```text
Giai đoạn 1 (giờ):     code trong guide này — transaction + trừ/hoàn kho
Giai đoạn 2 (tải tăng): Atomic conditional UPDATE chặn oversell + Idempotency-Key
Giai đoạn 3 (lớn):     Outbox + message broker cho side-effect; projection/keyset cho đọc
Giai đoạn 4 (flash sale): Redis reservation cho kho + batch sinh mã + rate limit
```

> Nguyên tắc xuyên suốt: **đo trước, tối ưu sau**. Mỗi bước trên đều thêm độ phức tạp vận hành — chỉ trả giá đó khi có số liệu (latency, lỗi oversell, QPS) chứng minh là cần.
