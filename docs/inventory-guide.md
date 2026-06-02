# Inventory Feature Guide

## Mục tiêu

Feature Inventory dùng để quản lý số lượng tồn kho hiện tại của từng sản phẩm trong hệ thống.

### Chức năng

- Tạo tồn kho cho một sản phẩm
- Xem danh sách tồn kho
- Xem tồn kho theo Id
- Xem tồn kho theo ProductId
- Cập nhật số lượng tồn kho
- Xóa tồn kho
- Đảm bảo mỗi sản phẩm chỉ có duy nhất một dòng tồn kho

---

# Thiết kế Database

## Inventory

### Mục đích

Lưu số lượng tồn kho hiện tại của sản phẩm. Mỗi `Product` chỉ tương ứng với đúng một dòng `inventories`.

### Bảng `inventories`

| Column      | Type        | Nullable | Description                                   |
|-------------|-------------|----------|-----------------------------------------------|
| id          | uuid        | NO       | Khóa chính (`InventoryId` — Typed ID)         |
| product_id  | uuid        | NO       | FK → `products.id` (`ProductId` — Typed ID)   |
| quantity    | bigint      | NO       | Số lượng tồn hiện tại (`long`, không âm)       |
| created_at  | timestamptz | NO       | Ngày tạo (UTC)                                |
| updated_at  | timestamptz | YES      | Ngày cập nhật gần nhất (UTC)                  |

### Index đề xuất

| Index                          | Columns       | Loại    | Mục đích                                       |
|--------------------------------|---------------|---------|------------------------------------------------|
| `ux_inventories_product_id`    | `product_id`  | UNIQUE  | Đảm bảo mỗi sản phẩm chỉ có một dòng tồn kho   |

> FK `product_id → products.id` cấu hình `OnDelete: Cascade` — khi xóa Product, Inventory tương ứng tự động bị xóa.

---

# Cấu trúc Domain (DDD)

```
backend/src/MinimalAPI.Domain/
├── Entities/
│   ├── Inventory.cs                # AggregateRoot<InventoryId>, sealed, factory Create()
│   └── InventoryId.cs              # readonly record struct InventoryId(Guid Value)
└── Interfaces/
    └── IInventoryRepository.cs     # chỉ cho Aggregate Root
```

## Quy tắc Domain

- `Inventory` là **Aggregate Root**, kế thừa `AggregateRoot<InventoryId>`, `sealed`.
- KHÔNG public setter — mọi thay đổi qua method (`Create`, `UpdateQuantity`).
- `InventoryId` và `ProductId` là **Typed ID** (`readonly record struct`) để type-safe.
- `Create(productId, quantity)` sinh `InventoryId.New()`, set `CreatedAt = UtcNow`.
- `UpdateQuantity(quantity)` cập nhật số lượng, set `UpdatedAt = UtcNow`; bỏ qua nếu giá trị không đổi.
- Invariant: `quantity` không được âm — vi phạm sẽ ném `DomainException` qua `EnsureQuantityIsValid`.

---

# Quan hệ

```text
(Product) 1 — 1 (Inventory)
```

### Lưu ý

```text
- Mỗi Product chỉ có duy nhất một Inventory record (đảm bảo bằng UNIQUE index trên product_id).
- Inventory không phải Aggregate Root độc lập về nghiệp vụ — nó luôn gắn với một Product tồn tại.
- Khi Product bị xóa thì Inventory tương ứng cũng bị xóa (cascade delete ở DB).
```

---

# Thiết kế API

## Endpoints

| Method | Route                                  | Description                          |
|--------|----------------------------------------|--------------------------------------|
| GET    | `/api/inventories`                     | Danh sách tồn kho                    |
| POST   | `/api/inventories`                     | Tạo tồn kho mới                      |
| GET    | `/api/inventories/{id}`                | Chi tiết tồn kho theo Id             |
| GET    | `/api/inventories/by-product/{productId}` | Tra cứu tồn kho theo ProductId    |
| PUT    | `/api/inventories/{id}`                | Cập nhật số lượng tồn kho            |
| DELETE | `/api/inventories/{id}`                | Xóa tồn kho                          |

### Ví dụ — Tạo tồn kho

> Lưu ý: trong `CreateInventoryCommand`, trường `id` chính là **ProductId** của sản phẩm cần tạo tồn kho.

```http
POST /api/inventories
```

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": 50
}
```

```json
{
  "isSuccess": true,
  "value": {
    "id": "8b1e...",
    "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "quantity": 50
  }
}
```

### Ví dụ — Cập nhật tồn kho

```http
PUT /api/inventories/{id}
```

```json
{
  "quantity": 100
}
```

## Vertical Slices (Application Layer)

```
backend/src/MinimalAPI.Application/Features/Inventories/
├── CreateInventory/
│   ├── CreateInventoryCommand.cs
│   ├── CreateInventoryHandler.cs
│   └── CreateInventoryValidator.cs
├── UpdateInventory/
│   ├── UpdateInventoryCommand.cs
│   ├── UpdateInventoryHandler.cs
│   └── UpdateInventoryValidator.cs
├── DeleteInventory/
│   ├── DeleteInventoryCommand.cs
│   └── DeleteInventoryHandler.cs
├── GetInventory/
│   ├── GetInventoryQuery.cs
│   └── GetInventoryHandler.cs
├── GetInventories/
│   ├── GetInventoriesQuery.cs
│   └── GetInventoriesHandler.cs
├── GetInventoryByProduct/
│   ├── GetInventoryByProductQuery.cs
│   └── GetInventoryByProductHandler.cs
└── DTOs/
    └── InventoryDto.cs              # (Guid Id, Guid ProductId, long Quantity)
```

Hạ tầng & API:

```
backend/src/MinimalAPI.Infrastructure/Persistence/
├── Configurations/InventoryConfiguration.cs   # ToTable("inventories"), HasConversion Typed ID, UNIQUE product_id
└── Repositories/InventoryRepository.cs

backend/src/MinimalAPI.Api/Endpoints/InventoryEndpoints.cs   # route group "/api/inventories" + WithTags("Inventories")
```

## Repository (`IInventoryRepository`)

Kế thừa `IRepository<Inventory, InventoryId>`, bổ sung:

| Method                       | Mục đích                                   |
|------------------------------|--------------------------------------------|
| `GetAllAsync`                | Lấy toàn bộ danh sách tồn kho              |
| `ExistsByProductIdAsync`     | Kiểm tra sản phẩm đã có tồn kho chưa       |
| `GetByProductIdAsync`        | Lấy tồn kho theo ProductId                 |

---

# Validation rules (FluentValidation)

| Field      | Rule                                                          |
|------------|---------------------------------------------------------------|
| `Id`       | Required (`NotEmpty`) — là ProductId của sản phẩm             |
| `Quantity` | Required, `>= 0` (không âm)                                   |

Kiểm tra nghiệp vụ bổ sung ở Handler:

| Rule                              | Mô tả                                               |
|-----------------------------------|------------------------------------------------------|
| Product phải tồn tại              | Tra `IProductRepository.GetByIdAsync` trước khi tạo |
| Mỗi Product chỉ có một Inventory  | Tra `ExistsByProductIdAsync` — đã có thì Failure    |

---

# Business Rules

- Inventory lưu số lượng tồn hiện tại của sản phẩm.
- Mỗi Product chỉ được tạo một Inventory; không cho phép tạo trùng `ProductId` (chặn ở Handler + UNIQUE index).
- Tạo/cập nhật phải đảm bảo `Quantity >= 0`, vi phạm invariant này ở domain sẽ ném `DomainException`.
- Update Inventory cập nhật `Quantity` hiện tại và set `UpdatedAt`.
- Delete Inventory xóa dòng tồn kho của sản phẩm.
- Khi Product bị xóa, Inventory tương ứng cũng bị xóa (cascade).
