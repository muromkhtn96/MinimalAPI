# Redis Cache Guide

## Mục tiêu

Hướng dẫn này giải thích **tại sao Redis quan trọng hơn code**, cách cấu hình đúng chuẩn, và quan trọng nhất — **quy tắc đặt tên key cache** để hệ thống không hỗn loạn khi có hàng nghìn bản ghi cache.

> 🎯 **Bài học quan trọng nhất**: Một hệ thống cache tốt không phải là "thêm cache vào nhiều chỗ nhất". Đó là: **biết chỗ nào cần, chỗ nào không — và khi nào phải xóa đi.**

---

# Tại sao Docs quan trọng hơn Code?

## Câu chuyện thực tế

Giả sử 6 tháng sau bạn hoặc đồng đội cần thêm feature mới. Họ thấy file `GetOrderByIdHandler.cs` có dòng:

```csharp
string cacheKey = $"order:id:{request.id}";
```

Sau đó họ viết `UpdateOrderStatusHandler` mới và **không biết** rằng phải xóa key này sau khi cập nhật. Kết quả: **cache trả về data cũ trong 5 phút** — đơn hàng vẫn hiện `Pending` dù đã `Confirmed`.

Nếu có docs này, developer mới sẽ đọc phần [Cache Invalidation](#4-cache-invalidation--quy-tắc-vàng), tra bảng và biết ngay phải gọi:

```csharp
await hybridCache.RemoveAsync(CacheKeys.OrderById(order.Id.Value), ct);
await hybridCache.RemoveAsync(CacheKeys.OrderByCode(order.Code), ct);
```

**Docs ngăn được bug. Code không tự làm được điều đó.**

---

# Kiến trúc Cache trong MinimalAPI

## Stack sử dụng

| Layer | Công nghệ | Vai trò |
|-------|-----------|---------|
| **L1 — In-process** | `MemoryCache` (tích hợp trong `HybridCache`) | Cache trong RAM của process, cực nhanh, tồn tại theo vòng đời request |
| **L2 — Distributed** | `Redis` (via `StackExchange.Redis`) | Cache chia sẻ giữa nhiều instance API, tồn tại qua restart |
| **Abstraction** | `HybridCache` (.NET 9+) | Wrapper thông minh: check L1 → miss → check L2 → miss → fetch từ DB → lưu cả 2 |

```text
Request
   │
   ▼
[HybridCache.GetOrCreateAsync]
   │
   ├─ L1 Hit? ──────────────────────────────────────────► Trả về (< 1ms)
   │
   ├─ L2 Hit (Redis)? ──────► Lưu vào L1 ──────────────► Trả về (1–5ms)
   │
   └─ Miss ──► Gọi DB ──► Lưu vào L2 (Redis) ──► Lưu vào L1 ──► Trả về (10–50ms)
```

### Tại sao cần cả 2 tầng?

- **L1 (Memory)**: Nhanh nhất có thể, không tốn network. Dữ liệu mất khi restart process.
- **L2 (Redis)**: Shared giữa nhiều pod/container. Không mất khi một instance restart.
- **Kết hợp**: Warm-up tự động — lần đầu lấy từ Redis (5ms), lần sau từ Memory (<1ms).

---

# Cấu hình Redis — Đúng chuẩn

## 1. Docker Compose (Development)

File `docker-compose.dev.yml` — Redis đã được khai báo sẵn:

```yaml
redis:
  image: redis:alpine
  container_name: RedisCache
  ports:
    - "6379:6379"
  restart: unless-stopped
```

Chạy lên bằng:

```bash
docker compose -f docker-compose.dev.yml up -d redis
```

Kiểm tra Redis đang chạy:

```bash
docker exec -it RedisCache redis-cli ping
# Kết quả mong đợi: PONG
```

## 2. Connection String

**`appsettings.Development.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;...",
    "Redis": "127.0.0.1:6379"
  }
}
```

**Production** (`appsettings.Production.json` hoặc environment variable):

```json
{
  "ConnectionStrings": {
    "Redis": "redis-host:6379,password=secret,ssl=true"
  }
}
```

> ⚠️ **Không bao giờ commit connection string production** vào git. Dùng environment variable hoặc secret manager.

## 3. Đăng ký Service (DI)

File [`DependencyInjection.cs`](file:///c:/Projects/MinimalAPI/backend/src/MinimalAPI.Infrastructure/DependencyInjection.cs):

```csharp
// L2: Redis Distributed Cache
services.AddStackExchangeRedisCache(options =>
{
    // Fallback về localhost nếu không có config
    options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
    
    // ⚠️ QUAN TRỌNG: InstanceName là prefix TỰ ĐỘNG ghép vào đầu key
    // Key thực tế trong Redis = "MinimalAPI_" + key bạn đặt
    // Ví dụ: "MinimalAPI_product:id:8b1e..."
    options.InstanceName = "MinimalAPI_";
});

// HybridCache (L1 + L2 wrapper)
services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),      // L2 (Redis) TTL
        LocalCacheExpiration = TimeSpan.FromMinutes(5)  // L1 (Memory) TTL
    };
});
```

### Giải thích các tham số

| Tham số | Giá trị | Ý nghĩa |
|---------|---------|---------|
| `InstanceName` | `"MinimalAPI_"` | Prefix tự động, phân biệt app khác nhau dùng cùng Redis |
| `Expiration` | 10 phút | Thời gian tồn tại trong Redis. Hết hạn → fetch lại DB |
| `LocalCacheExpiration` | 5 phút | Thời gian tồn tại trong RAM. Hết hạn → fallback xuống Redis |

> 💡 `LocalCacheExpiration` phải ≤ `Expiration`. Nếu L1 sống lâu hơn L2, bạn sẽ serve data từ L1 trong khi L2 đã bị invalidate — đây là nguồn gốc của bug stale data.

---

# Quy tắc đặt tên Key — PHẦN QUAN TRỌNG NHẤT

## Vấn đề nếu không có quy tắc

Không có convention → mỗi người đặt key theo ý riêng:

```text
"order_8b1e"          ← developer A
"Order:8b1e"          ← developer B  
"orders:id:8b1e"      ← developer C (số nhiều)
"order:8b1e...-full"  ← developer D (suffix tùy tiện)
```

Kết quả:
- Không ai biết key nào đang tồn tại trong Redis
- Invalidation sai (xóa nhầm key hoặc bỏ sót)
- Debug mất hàng giờ

## Convention đã chọn: `{domain}:{loại}:{định_danh}`

```text
{domain}       = product | customer | order | category
{loại}         = id | code
{định_danh}    = {uuid} | {code_value}
```

> 💡 Chỉ cache **chi tiết theo ID hoặc Code** — không cache danh sách. Lý do xem mục [Phân tích: Chỗ nào Cache](#phân-tích-chỗ-nào-cache-chỗ-nào-không).

### Ví dụ đầy đủ

| Key | Ý nghĩa | TTL |
|-----|---------|-----|
| `product:id:8b1e-...` | Chi tiết sản phẩm theo UUID | 10 phút |
| `product:code:SP00001` | Chi tiết sản phẩm theo mã | 10 phút |
| `customer:id:9c1f-...` | Chi tiết khách hàng theo UUID | 10 phút |
| `customer:code:KH00001` | Chi tiết khách hàng theo mã | 10 phút |
| `order:id:f4d2-...` | Chi tiết đơn hàng theo UUID | 5 phút |
| `order:code:DH00001` | Chi tiết đơn hàng theo mã | 5 phút |
| `category:id:3fa8-...` | Chi tiết danh mục | 30 phút |

> ⚠️ Key thực tế trong Redis sẽ là `MinimalAPI_{key}` do `InstanceName` prefix. Khi check bằng `redis-cli`, bạn sẽ thấy `MinimalAPI_product:id:8b1e...`.

## Nguồn sự thật duy nhất: `CacheKeys.cs`

**KHÔNG BAO GIỜ** hardcode key trực tiếp trong handler. Tất cả key phải đi qua [`CacheKeys.cs`](file:///c:/Projects/MinimalAPI/backend/src/MinimalAPI.Application/Abstractions/CacheKeys.cs):

```csharp
// ❌ SAI — hardcode trong handler
string cacheKey = $"order:id:{request.id}";

// ✅ ĐÚNG — dùng CacheKeys
string cacheKey = CacheKeys.OrderById(request.id);
```

### Nội dung `CacheKeys.cs`

```csharp
public static class CacheKeys
{
    // PRODUCT — chỉ cache chi tiết theo ID và Code
    public static string ProductById(Guid id)       => $"product:id:{id}";
    public static string ProductByCode(string code) => $"product:code:{code}";

    // CUSTOMER — chỉ cache chi tiết theo ID và Code
    public static string CustomerById(Guid id)       => $"customer:id:{id}";
    public static string CustomerByCode(string code) => $"customer:code:{code}";

    // ORDER — chỉ cache chi tiết theo ID và Code
    public static string OrderById(Guid id)       => $"order:id:{id}";
    public static string OrderByCode(string code) => $"order:code:{code}";

    // CATEGORY — chỉ cache chi tiết theo ID
    public static string CategoryById(Guid id) => $"category:id:{id}";
}
```

**Khi thêm key mới, chỉ cần sửa 1 file này.** Mọi handler tự động dùng key đúng.

---

# Base Code — Handler mẫu

> Phần này liệt kê code thực tế của từng loại handler theo domain. Đọc một lần để nắm pattern, sau đó áp dụng khi viết feature mới.

## Pattern 1 — READ handler (GetOrCreate)

Đây là pattern **đọc dữ liệu** — dùng `GetOrCreateAsync`: nếu có cache thì trả ngay, nếu không thì fetch DB rồi lưu vào cache.

### `GetProductHandler` — đọc theo ID

```csharp
// File: Application/Features/Products/GetProduct/GetProductHandler.cs
public sealed class GetProductHandler(
    IProductRepository productRepository,
    HybridCache hybridCache)                        // inject HybridCache
    : IRequestHandler<GetProductQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken ct)
    {
        // ① Lấy key từ CacheKeys — KHÔNG hardcode chuỗi
        var cacheKey = CacheKeys.ProductById(request.Id);

        // ② GetOrCreateAsync: cache hit → trả luôn | miss → chạy factory → lưu cache
        return await hybridCache.GetOrCreateAsync<ProductDto?>(
            cacheKey,
            async token =>
            {
                // Factory chỉ chạy khi CACHE MISS
                var productId = new ProductId(request.Id);
                var product = await productRepository.GetByIdAsync(productId, token);

                return product is not null
                    ? new ProductDto(
                        product.Id.Value,
                        product.Code,
                        product.Name.Value,
                        product.Price.Amount,
                        product.Price.Currency,
                        product.CategoryId.Value,
                        product.Category.Name,
                        product.Description,
                        product.IsActive,
                        product.CreatedAt)
                    : null;
            },
            cancellationToken: ct);
    }
}
```

> 🔑 **Điểm quan trọng**: `GetOrCreateAsync` là **thread-safe** — nếu 100 request cùng lúc miss cache, chỉ **1** request chạy factory, 99 cái còn lại chờ kết quả. Tránh được **cache stampede**.

### `GetProductActiveHandler` — đọc danh sách (KHÔNG cache)

Danh sách sản phẩm active **không được cache** vì:
- Số lượng sản phẩm active thay đổi khi `UpdateProductActive` / `DeleteProduct` được gọi — phải invalidate thường xuyên.
- Tổ hợp tìm kiếm / filter làm số lượng key có thể rất lớn, khó quản lý.

Thay vào đó, handler đọc thẳng từ DB mỗi lần:

```csharp
// File: Application/Features/Products/GetProductActive/GetProductActiveHandler.cs
public sealed class GetProductActiveHandler(
    IProductRepository productRepo)
    : IRequestHandler<GetProductActiveQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductActiveQuery request, CancellationToken ct)
    {
        // KHÔNG dùng hybridCache — đọc thẳng từ DB
        var products = await productRepo.GetActiveProductsAsync(ct);
        var dtos = products
            .Select(p => new ProductDto(
                p.Id.Value, p.Code, p.Name.Value,
                p.Price.Amount, p.Price.Currency,
                p.CategoryId.Value, p.Category.Name,
                p.Description, p.IsActive, p.CreatedAt))
            .ToList();

        return Result<List<ProductDto>>.Success(dtos);
    }
}
```

> 💡 Tốc độ chấp nhận được nhờ PostgreSQL index trên cột `IsActive`. Cache danh sách sẽ mang lại phức tạp không cần thiết.

### `GetOrderByIdHandler` — đọc entity phức hợp (kèm join)

```csharp
// File: Application/Features/Orders/GetOrderById/GetOrderByIdHandler.cs
// Đây là ví dụ cache entity cần JOIN nhiều bảng — tiết kiệm nhất
public sealed class GetOrderByIdHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    HybridCache hybridCache)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        string cacheKey = CacheKeys.OrderById(request.id);  // "order:id:{uuid}"

        var orderDto = await hybridCache.GetOrCreateAsync<OrderDto?>(
            cacheKey,
            async token =>
            {
                var order = await orderRepository.GetByOrderIdAsync(new OrderId(request.id), token);
                if (order is null) return null;

                // JOIN Customer
                var customer = await customerRepository.GetByIdAsync(order.CustomerId, token);
                var customerName = customer?.FullName ?? "Khách vô danh";

                // JOIN Product cho từng dòng hàng
                var detailDtos = new List<OrderDetailDto>();
                foreach (var detail in order.Details)
                {
                    var product = await productRepository.GetByIdAsync(detail.ProductId, token);
                    detailDtos.Add(new OrderDetailDto(
                        detail.ProductId.Value,
                        product?.Name.Value ?? "Sản phẩm không xác định",
                        detail.Quantity,
                        detail.UnitPrice.Amount,
                        detail.LineTotal.Amount));
                }

                return new OrderDto(
                    order.Id.Value, order.Code,
                    order.CustomerId.Value, customerName,
                    order.Status.ToString(),
                    order.TotalAmount.Amount, order.TotalAmount.Currency,
                    order.Note, order.CreatedAt, detailDtos);
            },
            cancellationToken: ct);

        return orderDto is not null
            ? Result<OrderDto>.Success(orderDto)
            : Result<OrderDto>.Failure("Đơn hàng không tồn tại.");
    }
}
```

> ⚠️ Cache này lưu cả `customerName` và `productName`. Nếu tên KH/SP đổi, order cache vẫn giữ tên cũ cho đến hết TTL (5 phút). Đây là **đánh đổi có chủ ý** — chấp nhận sai sót nhỏ để đổi lấy hiệu năng. Nếu nghiệp vụ yêu cầu tên luôn đúng realtime, phải bỏ cache cho handler này.

---

## Pattern 2 — WRITE handler (Update + Invalidate)

Mọi handler ghi đều theo **một** luồng cố định: Commit DB → Invalidate cache → Return.

### `UpdateProductHandler` — cập nhật + xóa cache

```csharp
// File: Application/Features/Products/UpdateProduct/UpdateProductHandler.cs
public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,                        // inject để invalidate
    ILogger<UpdateProductHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null) return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");

        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
        if (category is null) return Result<ProductDto>.Failure("Danh mục không tồn tại.");

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            product.UpdateInfo(ProductName.Create(request.Name), new CategoryId(request.CategoryId), request.Description);
            product.UpdatePrice(Money.Create(request.Price, request.Currency));

            // ① Commit DB trước
            await unitOfWork.CommitAsync(ct);

            // ② Invalidate cache SAU KHI commit thành công
            // Chỉ xóa các key chi tiết — không có list cache
            await hybridCache.RemoveAsync(CacheKeys.ProductById(product.Id.Value), ct);
            await hybridCache.RemoveAsync(CacheKeys.ProductByCode(product.Code), ct);

            logger.LogInformation("Sản phẩm {ProductId} đã được cập nhật", product.Id.Value);

            // ③ Trả response
            return Result<ProductDto>.Success(new ProductDto(
                product.Id.Value, product.Code, product.Name.Value,
                product.Price.Amount, product.Price.Currency,
                product.CategoryId.Value, category.Name,
                product.Description, product.IsActive, product.CreatedAt));
        }
        catch (Exception ex)
        {
            // Nếu commit lỗi → rollback, KHÔNG xóa cache (cache vẫn đúng)
            logger.LogError(ex, "Cập nhật sản phẩm {ProductId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

### `DeleteProductHandler` — xóa entity + dọn sạch cache

```csharp
// File: Application/Features/Products/DeleteProduct/DeleteProductHandler.cs
public sealed class DeleteProductHandler(
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<DeleteProductHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null) return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");

        // Lưu lại TRƯỚC KHI xóa — sau Remove() entity không còn accessible
        var productCode = product.Code;

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            productRepo.Remove(product);
            await unitOfWork.CommitAsync(ct);

            // Chỉ xóa các key chi tiết — không có list cache
            await hybridCache.RemoveAsync(CacheKeys.ProductById(request.Id), ct);
            await hybridCache.RemoveAsync(CacheKeys.ProductByCode(productCode), ct);

            logger.LogInformation("Đã xóa sản phẩm {ProductId} '{Name}'",
                product.Id.Value, product.Name.Value);

            return Result<ProductDto>.Success(new ProductDto(
                product.Id.Value, product.Code, product.Name.Value,
                product.Price.Amount, product.Price.Currency,
                product.CategoryId.Value, product.Category.Name,
                product.Description, product.IsActive, product.CreatedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xóa sản phẩm {ProductId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

---

## Pattern 3 — CHANGE-STATUS handler (Active/Deactive)

Khi chỉ đổi trạng thái, chỉ cần xóa cache chi tiết theo ID và Code của sản phẩm đó.

> 💡 Vì không có `product:list:active` / `product:list:deactive`, logic invalidation đơn giản hơn nhiều — chỉ cần xóa đúng 2 key của entity.

### `UpdateProductActiveHandler` — kích hoạt sản phẩm

```csharp
// File: Application/Features/Products/UpdateProductActive/UpdateProductActiveHandler.cs
public sealed class UpdateProductActiveHandler(
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<UpdateProductActiveHandler> logger)
    : IRequestHandler<UpdateProductActiveCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductActiveCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null) return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");

        // Business rule: tối đa 10 sản phẩm active
        var activeProducts = await productRepo.GetActiveProductsAsync(ct);
        if (!product.IsActive && activeProducts.Count >= 10)
            return Result<ProductDto>.Failure("Không thể kích hoạt. Đã có 10 sản phẩm đang hoạt động.");

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            product.Activate();
            await unitOfWork.CommitAsync(ct);

            // Chỉ xóa cache chi tiết — không có list cache
            await hybridCache.RemoveAsync(CacheKeys.ProductById(product.Id.Value), ct);
            await hybridCache.RemoveAsync(CacheKeys.ProductByCode(product.Code), ct);

            logger.LogInformation("Đã kích hoạt sản phẩm {ProductId} '{Name}'",
                product.Id.Value, product.Name.Value);

            return Result<ProductDto>.Success(new ProductDto(
                product.Id.Value, product.Code, product.Name.Value,
                product.Price.Amount, product.Price.Currency,
                product.CategoryId.Value, product.Category.Name,
                product.Description, product.IsActive, product.CreatedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kích hoạt sản phẩm {ProductId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

---

## Pattern 4 — ORDER status handler (Confirm/Cancel + Invalidate)

Đơn hàng thay đổi trạng thái thường có side-effects (trừ/hoàn kho). Cache phải được xóa SAU toàn bộ transaction commit.

### `ConfirmOrderHandler` — xác nhận đơn + trừ kho + xóa cache

```csharp
// File: Application/Features/Orders/ConfirmOrder/ConfirmOrderHandler.cs
public sealed class ConfirmOrderHandler(
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IInventoryRepository inventoryRepo,
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<ConfirmOrderHandler> logger)
    : IRequestHandler<ConfirmOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var order = await orderRepo.GetByOrderIdAsync(new OrderId(request.Id), ct);
        if (order is null) return Result<OrderDto>.Failure("Đơn hàng không tồn tại.");
        if (order.Status == OrderStatus.Confirmed) return Result<OrderDto>.Failure("Đơn hàng đã xác nhận trước đó.");
        if (order.Status == OrderStatus.Cancelled) return Result<OrderDto>.Failure("Không thể xác nhận đơn đã hủy.");

        var details = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);
        if (details.Count == 0) return Result<OrderDto>.Failure("Không thể xác nhận đơn hàng rỗng.");

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            // Kiểm tra và trừ kho
            var entities = new List<Inventory>();
            foreach (var item in details)
            {
                var inventory = await inventoryRepo.GetByProductIdAsync(item.ProductId, ct);
                if (inventory is null)
                    return Result<OrderDto>.Failure($"Sản phẩm {item.ProductId.Value} chưa có tồn kho.");
                if (inventory.Quantity < item.Quantity)
                    return Result<OrderDto>.Failure(
                        $"Không đủ tồn kho. Còn {inventory.Quantity}, cần {item.Quantity}.");

                inventory.UpdateQuantity(inventory.Quantity - item.Quantity);
                entities.Add(inventory);
            }
            inventoryRepo.UpdateRange(entities);

            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.UtcNow;
            order.AddConfirmedEvent();
            orderRepo.Update(order);

            // Commit toàn bộ (đổi status + trừ kho) trong 1 transaction
            await unitOfWork.CommitAsync(ct);

            // Invalidate sau khi commit thành công — chỉ xóa order cache
            // (Inventory không cache nên không cần xóa)
            await hybridCache.RemoveAsync(CacheKeys.OrderById(order.Id.Value), ct);
            await hybridCache.RemoveAsync(CacheKeys.OrderByCode(order.Code), ct);

            logger.LogInformation("Xác nhận đơn hàng {Id} thành công, đã trừ tồn kho", order.Id.Value);

            // Build response DTO...
            var customer = await customerRepo.GetByIdAsync(order.CustomerId, ct);
            var products = new List<Product>();
            foreach (var item in details)
            {
                var p = await productRepo.GetByIdAsync(item.ProductId, ct);
                if (p is not null) products.Add(p);
            }

            var detailDtos = details.Select(item =>
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                return new OrderDetailDto(
                    item.ProductId.Value,
                    product?.Name.Value ?? "Sản phẩm không xác định",
                    item.Quantity, item.UnitPrice.Amount, item.LineTotal.Amount);
            }).ToList();

            return Result<OrderDto>.Success(new OrderDto(
                order.Id.Value, order.Code,
                order.CustomerId.Value, customer?.FullName ?? "Khách vô danh",
                order.Status.ToString(),
                order.TotalAmount.Amount, order.TotalAmount.Currency,
                order.Note, order.CreatedAt, detailDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xác nhận đơn {OrderId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

### `CancelOrderHandler` — hủy đơn + hoàn kho + xóa cache

```csharp
// File: Application/Features/Orders/CancelOrder/CancelOrderHandler.cs
public sealed class CancelOrderHandler(
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IInventoryRepository inventoryRepo,
    ICustomerRepository customerRepo,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<CancelOrderHandler> logger)
    : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await orderRepo.GetByOrderIdAsync(new OrderId(request.Id), ct);
        if (order is null) return Result<OrderDto>.Failure("Đơn hàng không tồn tại.");
        if (order.Status == OrderStatus.Cancelled) return Result<OrderDto>.Failure("Đơn hàng đã bị hủy trước đó.");

        var customerName = (await customerRepo.GetByIdAsync(order.CustomerId, ct))?.FullName ?? "Khách vô danh";
        var previousStatus = order.Status;

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            // Chỉ hoàn kho nếu đơn đã Confirmed (đã trừ kho)
            // Đơn Pending chưa trừ kho → KHÔNG hoàn (tránh cộng dư)
            if (previousStatus == OrderStatus.Confirmed)
            {
                var details = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);
                var entities = new List<Inventory>();
                foreach (var item in details)
                {
                    var inventory = await inventoryRepo.GetByProductIdAsync(item.ProductId, ct);
                    if (inventory is null) return Result<OrderDto>.Failure("Không tìm thấy tồn kho.");
                    inventory.UpdateQuantity(inventory.Quantity + item.Quantity); // hoàn kho
                    entities.Add(inventory);
                }
                inventoryRepo.UpdateRange(entities);
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            order.AddCancelledEvent();
            orderRepo.Update(order);

            await unitOfWork.CommitAsync(ct);

            // Invalidate sau commit
            await hybridCache.RemoveAsync(CacheKeys.OrderById(order.Id.Value), ct);
            await hybridCache.RemoveAsync(CacheKeys.OrderByCode(order.Code), ct);

            logger.LogInformation("Hủy đơn hàng {Id} thành công", order.Id.Value);

            var allDetails = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);
            var detailDtos = allDetails.Select(item =>
                new OrderDetailDto(item.ProductId.Value, string.Empty,
                    item.Quantity, item.UnitPrice.Amount, item.LineTotal.Amount)).ToList();

            return Result<OrderDto>.Success(new OrderDto(
                order.Id.Value, order.Code,
                order.CustomerId.Value, customerName,
                order.Status.ToString(),
                order.TotalAmount.Amount, order.TotalAmount.Currency,
                order.Note, order.CreatedAt, detailDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hủy đơn {OrderId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

---

## Pattern 5 — CUSTOMER handler (Read + Write)

### `GetCustomerByIdHandler` — đọc khách hàng theo ID

```csharp
// File: Application/Features/Customers/GetCustomerById/GetCustomerByIdHandler.cs
public sealed class GetCustomerHandler(
    ICustomerRepository customerRepository,
    HybridCache hybridCache)
    : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.CustomerById(request.Id);  // "customer:id:{uuid}"

        var dto = await hybridCache.GetOrCreateAsync<CustomerDto?>(
            cacheKey,
            async token =>
            {
                var customer = await customerRepository.GetByIdAsync(new CustomerId(request.Id), token);
                if (customer is null) return null;

                return new CustomerDto(
                    customer.Id.Value, customer.Code, customer.FullName,
                    customer.Email, customer.Phone, customer.Type,
                    customer.TaxCode, customer.Gender, customer.DateOfBirth,
                    customer.Address, customer.Note,
                    customer.IsActive, customer.CreatedAt, customer.UpdateAt);
            },
            cancellationToken: ct);

        return dto is not null
            ? Result<CustomerDto>.Success(dto)
            : Result<CustomerDto>.Failure("Khách hàng không tồn tại.");
    }
}
```

### `UpdateCustomerHandler` — cập nhật + xóa cache

```csharp
// File: Application/Features/Customers/UpdateCustomer/UpdateCustomerHandler.cs
public sealed class UpdateCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<UpdateCustomerHandler> logger)
    : IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await customerRepository.GetByIdAsync(new CustomerId(request.Id), ct);
        if (customer is null) return Result<CustomerDto>.Failure("Khách hàng không tồn tại.");

        // Kiểm tra trùng SĐT nếu người dùng đổi số
        if (!string.IsNullOrWhiteSpace(request.Phone) && customer.Phone != request.Phone)
        {
            var phoneExists = await customerRepository.ExistsByPhoneAsync(request.Phone, ct);
            if (phoneExists) return Result<CustomerDto>.Failure("Số điện thoại đã được sử dụng.");
        }

        var customerCode = customer.Code; // lưu trước khi UpdateInfo
        customer.UpdateInfo(request.FullName, request.Phone, request.TaxCode,
            request.Gender, request.DateOfBirth, request.Address, request.Note);

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            await unitOfWork.CommitAsync(ct);

            // Xóa cả 2 key — by ID và by Code
            await hybridCache.RemoveAsync(CacheKeys.CustomerById(request.Id), ct);
            await hybridCache.RemoveAsync(CacheKeys.CustomerByCode(customerCode), ct);

            logger.LogInformation("Cập nhật khách hàng {Code} thành công", customer.Code);

            return Result<CustomerDto>.Success(new CustomerDto(
                customer.Id.Value, customer.Code, customer.FullName,
                customer.Email, customer.Phone, customer.Type,
                customer.TaxCode, customer.Gender, customer.DateOfBirth,
                customer.Address, customer.Note,
                customer.IsActive, customer.CreatedAt, customer.UpdateAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cập nhật khách hàng {Id} thất bại", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
```

---

## Tóm tắt pattern theo bảng

| Pattern | Handler | Cache action | Thứ tự bắt buộc |
|---------|---------|--------------|-----------------|
| **Read** | `GetXxxHandler` | `GetOrCreateAsync(key, factory)` | Factory chỉ chạy khi miss |
| **Write — Update** | `UpdateXxxHandler` | `RemoveAsync(id_key, code_key)` | Commit DB **→** Remove cache |
| **Write — Delete** | `DeleteXxxHandler` | `RemoveAsync(id_key, code_key)` | Commit DB **→** Remove cache |
| **Write — Create** | `CreateXxxHandler` | Không cần xóa (entity mới, chưa có cache) | — |
| **Change status** | `Activate/Deactivate` | `RemoveAsync(id_key, code_key)` | Commit DB **→** Remove cache |
| **Order status** | `Confirm/CancelOrder` | `RemoveAsync(id_key, code_key)` | Commit DB (+ kho) **→** Remove cache |

> 🔑 **Quy tắc duy nhất cần nhớ**: `RemoveAsync` phải đứng **sau** `CommitAsync`. Không bao giờ đảo ngược.

---

# Phân tích: Chỗ nào Cache, chỗ nào không?

> **Nguyên tắc vàng**: Chỉ cache dữ liệu **đọc nhiều, thay đổi ít** và khi **chi phí fetch từ DB đáng kể**.

## ✅ NÊN Cache

### Product — Cache chi tiết, KHÔNG cache danh sách

| Query | Key | Lý do Cache |
|-------|-----|-------------|
| `GET /api/products/{id}` | `product:id:{id}` | Catalog sản phẩm ổn định, đọc rất nhiều lần (tạo đơn, hiển thị) |
| `GET /api/products/code/{code}` | `product:code:{code}` | Tra cứu theo mã, pattern đọc nhiều |

### Customer — Cache chi tiết, KHÔNG cache danh sách

| Query | Key | Lý do Cache |
|-------|-----|-------------|
| `GET /api/customers/{id}` | `customer:id:{id}` | Tạo đơn luôn lookup customer, cache giảm tải |
| `GET /api/customers/code/{code}` | `customer:code:{code}` | Tra cứu theo mã, đọc nhiều |

### Order — Cache chi tiết, TTL ngắn

| Query | Key | Lý do Cache |
|-------|-----|-------------|
| `GET /api/orders/{id}` | `order:id:{id}` | Chi tiết đơn hay được xem lại (tracking status) |
| `GET /api/orders/code/{code}` | `order:code:{code}` | Tra cứu theo mã đơn — dùng thường xuyên |

### Category — Cache chi tiết, TTL dài nhất

| Query | Key | Lý do Cache |
|-------|-----|-------------|
| `GET /api/categories/{id}` | `category:id:{id}` | Category thay đổi rất hiếm, TTL 30 phút hợp lý |

## ❌ KHÔNG NÊN Cache

| Query | Lý do KHÔNG cache |
|-------|------------------|
| `GET /api/products/active` | Status sản phẩm thay đổi khi Activate/Deactivate → invalidation thường xuyên. Danh sách nhỏ, DB index đủ nhanh. |
| `GET /api/products/deactive` | Cùng lý do với `active`. |
| `GET /api/products/by-category/{id}` | Invalidation phức tạp — khi cập nhật/xóa/thêm sản phẩm phải biết category nào bị ảnh hưởng. |
| `GET /api/categories` (danh sách) | Số lượng danh mục ít, đọc DB nhanh. Không cần thêm phức tạp. |
| `GET /api/orders` (danh sách phân trang) | Kết quả phụ thuộc `page`, `pageSize`, `search` → vô số tổ hợp key, chiếm RAM/Redis vô ích. Dùng DB index tốt thay vì cache. |
| `GET /api/customers` (danh sách phân trang) | Cùng lý do với orders. Search + filter + page = không thể cache hiệu quả. |
| Inventory quantity | Tồn kho thay đổi theo từng đơn hàng. Cache sai số tồn kho = oversell. **Tuyệt đối không cache.** |
| POST / PUT / DELETE handlers | Write operations không cache kết quả, chỉ invalidate cache sau khi commit. |
| `CreateOrder` flow | Phải đọc tồn kho realtime (race condition). Cache ở đây = nguy hiểm. |

---

# Cache Invalidation — Quy tắc Vàng

> "There are only two hard things in Computer Science: cache invalidation and naming things." — Phil Karlton

## Nguyên tắc

**Xóa cache SAU KHI commit thành công, TRƯỚC KHI trả response.**

```csharp
// ✅ ĐÚNG — thứ tự chuẩn
await unitOfWork.CommitAsync(ct);                           // 1. Commit DB
await hybridCache.RemoveAsync(CacheKeys.OrderById(...), ct); // 2. Xóa cache
return Result.Success(...);                                  // 3. Trả response

// ❌ SAI — xóa cache trước commit
await hybridCache.RemoveAsync(CacheKeys.OrderById(...), ct); // Cache trống
await unitOfWork.CommitAsync(ct);                           // Nếu lỗi ở đây → DB rollback
// Kết quả: cache đã xóa nhưng DB chưa update → lần sau read cache miss → load DB cũ
```

## Bảng Invalidation đầy đủ

### Product

| Event (write handler) | Keys phải xóa |
|-----------------------|---------------|
| `CreateProduct` | Không cần (sản phẩm mới, chưa có cache) |
| `UpdateProduct` | `product:id:{id}`, `product:code:{code}` |
| `DeleteProduct` | `product:id:{id}`, `product:code:{code}` |
| `UpdateProductActive` | `product:id:{id}`, `product:code:{code}` |
| `UpdateProductDeactive` | `product:id:{id}`, `product:code:{code}` |

### Customer

| Event (write handler) | Keys phải xóa |
|-----------------------|---------------|
| `CreateCustomer` | Không cần (chưa có key nào liên quan) |
| `UpdateCustomer` | `customer:id:{id}`, `customer:code:{code}` |
| `DeleteCustomer` | `customer:id:{id}`, `customer:code:{code}` |
| `ActivateCustomer` | `customer:id:{id}`, `customer:code:{code}` |
| `DeactivateCustomer` | `customer:id:{id}`, `customer:code:{code}` |

### Category

| Event (write handler) | Keys phải xóa |
|-----------------------|---------------|
| `CreateCategory` | Không cần (danh mục mới, chưa có cache) |
| `UpdateCategory` | `category:id:{id}` |
| `DeleteCategory` | `category:id:{id}` |

### Order

| Event (write handler) | Keys phải xóa |
|-----------------------|---------------|
| `CreateOrder` | Không cần (đơn mới, chưa có cache) |
| `UpdateOrder` | `order:id:{id}`, `order:code:{code}` |
| `ConfirmOrder` | `order:id:{id}`, `order:code:{code}` |
| `CancelOrder` | `order:id:{id}`, `order:code:{code}` |

---

# TTL Strategy — Tại sao các domain dùng TTL khác nhau?

| Domain | L1 TTL | L2 TTL | Lý do |
|--------|--------|--------|-------|
| Category | 5 phút | 30 phút | Thay đổi cực hiếm. Admin thêm danh mục mới = event hiếm. |
| Product | 5 phút | 10 phút | Tương đối ổn định. Giá có thể thay đổi nhưng không liên tục. |
| Customer | 5 phút | 10 phút | Thông tin KH ổn định. Cập nhật address, phone = hiếm. |
| Order | 5 phút | 5 phút | Trạng thái thay đổi thường xuyên hơn. TTL ngắn = ít stale hơn. |
| Inventory | ❌ | ❌ | Không cache — realtime critical. |

> 💡 TTL là **"lưới an toàn cuối"** — nếu invalidation bị miss vì bug, cache cũ cũng sẽ tự expire. Càng ngắn thì càng an toàn hơn nhưng tốn DB hơn.

---

# Verify Cache đang hoạt động

## Bước 1: Kết nối Redis CLI

```bash
docker exec -it RedisCache redis-cli
```

## Bước 2: Xem tất cả key của MinimalAPI

```bash
# Liệt kê tất cả key (không nên dùng trên production với nhiều key)
KEYS MinimalAPI_*

# Xem chi tiết một key
GET "MinimalAPI_product:id:8b1e..."

# Xem TTL còn lại (tính bằng giây)
TTL "MinimalAPI_product:id:8b1e..."

# Xem số key đang tồn tại
DBSIZE
```

## Bước 3: Test Cache Hit vs Miss

Gọi API lần 1 (Cache Miss — đi xuống DB):

```bash
curl http://localhost:5000/api/products/SP00001
# Header response: X-Cache: MISS (nếu có middleware)
# Log: "DB query executed"
```

Gọi API lần 2 (Cache Hit — trả từ Redis):

```bash
curl http://localhost:5000/api/products/SP00001
# Nhanh hơn rõ rệt (~5ms vs ~50ms)
```

Kiểm tra trong Redis:

```bash
GET "MinimalAPI_product:code:SP00001"
# Sẽ thấy JSON đã được serialize
```

## Bước 4: Test Invalidation

```bash
# 1. Lấy sản phẩm (cache được warm)
curl http://localhost:5000/api/products/SP00001

# 2. Kiểm tra key trong Redis
redis-cli GET "MinimalAPI_product:code:SP00001"
# → thấy data

# 3. Update sản phẩm
curl -X PUT http://localhost:5000/api/products/{id} -d '{"name": "Tên mới", ...}'

# 4. Kiểm tra lại Redis — key phải đã bị xóa
redis-cli GET "MinimalAPI_product:code:SP00001"
# → (nil) → Invalidation hoạt động đúng

# 5. Gọi lại GET — phải fetch từ DB (cache miss), trả data mới
curl http://localhost:5000/api/products/SP00001
# → Thấy tên mới
```

---

# Thêm feature mới — Checklist

Mỗi khi viết một feature query mới có cache, phải làm đủ các bước:

```text
□ 1. Thêm key vào CacheKeys.cs
     → Đặt tên đúng format: {domain}:{loại}:{định_danh}
     → Comment TTL dự kiến và ai invalidate

□ 2. Wrap query bằng hybridCache.GetOrCreateAsync(CacheKeys.XxxYyy(...), ...)
     → Không hardcode chuỗi key

□ 3. Cập nhật bảng Invalidation trong docs này
     → Ghi rõ feature nào phải xóa key nào

□ 4. Thêm RemoveAsync vào TẤT CẢ write handlers liên quan
     → PHẢI làm NGAY, đừng để "todo sau"
     → Thứ tự: CommitAsync → RemoveAsync → return

□ 5. Test thủ công bằng redis-cli hoặc unit test
     → Cache miss lần đầu
     → Cache hit lần 2
     → Invalidation xóa đúng key
```

---

# Anti-patterns cần tránh

## 1. Cache nested objects với foreign keys

```csharp
// ❌ SAI — cache OrderDto kèm CustomerName và ProductName
// Nếu tên KH/SP thay đổi, phải invalidate order cache — phức tạp và dễ miss
var orderDto = new OrderDto(order.Id, ..., customer.FullName, ...);
await hybridCache.SetAsync(CacheKeys.OrderById(id), orderDto);
```

Cách xử lý: Cache object "phẳng" — chỉ id, không embed related data. Hoặc chấp nhận TTL ngắn.

## 2. Cache danh sách phân trang

```csharp
// ❌ SAI — cache với page/pageSize trong key
string key = $"order:list:page:{request.Page}:size:{request.PageSize}:search:{request.Search}";
// Tổ hợp key = vô tận, không invalidate được hiệu quả
```

Giải pháp: Dùng DB index tốt thay vì cache danh sách dài.

## 3. Cache sau khi commit thất bại

```csharp
// ❌ SAI — không check commit result
await unitOfWork.CommitAsync(ct); // có thể throw
await hybridCache.SetAsync(key, dto); // nếu commit lỗi, cache vẫn được set → stale data
```

Cách fix: `try { commit; cache; } catch { rollback; throw; }`

## 4. Không thống nhất key giữa Set và Remove

```csharp
// Set với key này
await hybridCache.GetOrCreateAsync("product:id:...", ...);

// Remove với key khác (typo!)
await hybridCache.RemoveAsync("products:id:...", ...); // "products" vs "product"
// → Cache không bao giờ bị invalidate!
```

**Cách fix duy nhất: dùng `CacheKeys.cs` cho cả hai.**

---

# Câu hỏi thường gặp (FAQ)

**Q: Redis die thì ứng dụng có die theo không?**

A: Không. `HybridCache` có graceful fallback — nếu Redis không có, nó bỏ qua L2 và đi thẳng xuống DB. App chạy chậm hơn nhưng không down.

---

**Q: Nên đặt TTL bao nhiêu?**

A: Không có con số tuyệt đối. Công thức đơn giản:
- Dữ liệu thay đổi hàng ngày → TTL = 5–10 phút
- Dữ liệu thay đổi hàng tuần → TTL = 30–60 phút
- Dữ liệu gần như không đổi → TTL = vài giờ
- Luôn implement invalidation — TTL chỉ là lưới an toàn cuối.

---

**Q: Có nên cache list tất cả đơn hàng (`GET /api/orders`) không?**

A: Không. Lý do:
1. Kết quả phụ thuộc `page`, `pageSize`, `search`, `status` → vô số tổ hợp
2. Khi tạo đơn mới hoặc thay đổi status → phải invalidate TẤT CẢ combination → không thể làm được
3. DB + index đủ nhanh cho list query thông thường

---

**Q: `InstanceName = "MinimalAPI_"` có bắt buộc không?**

A: Không bắt buộc về mặt kỹ thuật. Nhưng **rất khuyến nghị** vì:
- Nhiều app dùng cùng Redis server → tránh đụng độ key
- Dễ filter khi debug: `KEYS MinimalAPI_*`
- Dễ flush cache của 1 app: `SCAN + DEL MinimalAPI_*`

---

**Q: Làm sao flush toàn bộ cache khi deploy version mới?**

A: Chạy command trong Redis CLI:

```bash
# Xóa tất cả key của MinimalAPI (production cẩn thận!)
redis-cli --scan --pattern "MinimalAPI_*" | xargs redis-cli DEL

# Hoặc flush toàn bộ DB (nguy hiểm nếu share Redis)
redis-cli FLUSHDB
```

> ⚠️ Trên production nhiều instance, flush cache sẽ gây **cache stampede** — mọi request đồng loạt xuống DB. Nên làm ngoài giờ cao điểm.

---

# Hướng mở rộng (khi hệ thống lớn)

> 📌 Phần này chỉ cần thiết khi có vấn đề performance thực tế. Đừng tối ưu sớm.

## Distributed lock với Redis (Redlock)

Khi nhiều instance cùng xử lý một request (race condition trên cùng resource):

```csharp
// Ý tưởng — không implement sẵn trong codebase
using var redlock = await redlockFactory.CreateLockAsync(
    resource: $"lock:inventory:{productId}",
    expiryTime: TimeSpan.FromSeconds(5));

if (redlock.IsAcquired)
{
    // critical section — chỉ 1 instance chạy tại một thời điểm
}
```

Dùng khi: Flash sale, tạo đơn hàng volume cao cho cùng sản phẩm.

## Cache-aside với Tag invalidation

Khi hệ thống mở rộng và cần cache danh sách (list cache), `HybridCache` hỗ trợ tags để xóa nhóm key cùng lúc:

```csharp
// HybridCache hỗ trợ tags (preview)
await hybridCache.GetOrCreateAsync(
    key: $"product:list:category:{categoryId}",
    factory: ...,
    tags: [$"category:{categoryId}", "product:list"],  // nhiều tag
    options: ...);

// Xóa tất cả key có tag này — 1 lệnh thay vì nhiều RemoveAsync
await hybridCache.RemoveByTagAsync($"category:{categoryId}");
```

> 📌 Hiện tại dự án **không dùng list cache**, nhưng nếu sau này cần mở rộng, đây là hướng đúng thay vì hardcode nhiều `RemoveAsync`.
