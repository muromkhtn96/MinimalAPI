# Product Feature Guide

## Mục tiêu

Feature Product dùng để quản lý thông tin sản phẩm (tên, giá, danh mục, mô tả, trạng thái) trong hệ thống.

### Chức năng

- Tạo sản phẩm mới
- Xem chi tiết sản phẩm theo Id
- Lấy danh sách sản phẩm (paged, search)
- Lấy danh sách sản phẩm theo danh mục
- Lấy danh sách sản phẩm đang / không hoạt động
- Cập nhật thông tin & giá sản phẩm
- Kích hoạt / vô hiệu hóa sản phẩm
- Xóa sản phẩm

---

# Thiết kế Database

## Product

### Mục đích

Lưu thông tin sản phẩm — bao gồm tên, giá (tiền tệ), danh mục, mô tả và trạng thái hoạt động.

### Bảng `products`

| Column          | Type           | Nullable | Description                                       |
|-----------------|----------------|----------|---------------------------------------------------|
| id              | uuid           | NO       | Khóa chính (`ProductId` — Typed ID)               |
| code            | varchar(20)    | NO       | Mã sản phẩm (unique, VD `SP00001`) — sinh tự động |
| name            | varchar(200)   | NO       | Tên sản phẩm — Value Object `ProductName`         |
| price_amount    | decimal(18,2)  | NO       | Số tiền — thành phần của VO `Money`               |
| price_currency  | varchar(3)     | NO       | Mã tiền tệ (VD `VND`, `USD`) — VO `Money`         |
| category_id     | uuid           | NO       | FK → `categories.id` (`CategoryId` — Typed ID)    |
| description     | text           | YES      | Mô tả sản phẩm                                    |
| is_active       | boolean        | NO       | Trạng thái hoạt động (default: `true`)            |
| created_at      | timestamptz    | NO       | Ngày tạo (UTC)                                    |
| updated_at      | timestamptz    | YES      | Ngày cập nhật gần nhất (UTC)                      |

### Index đề xuất

| Index                       | Columns        | Loại    | Mục đích                              |
|-----------------------------|----------------|---------|---------------------------------------|
| `ux_products_code`          | `code`         | UNIQUE  | Tra cứu nhanh theo mã, đảm bảo unique |
| `ux_products_name`          | `name`         | UNIQUE  | Tên sản phẩm không trùng (xem lưu ý)  |
| `ix_products_category_id`   | `category_id`  | BTREE   | Lọc / join theo danh mục              |
| `ix_products_is_active`     | `is_active`    | BTREE   | Lọc sản phẩm active / deactive        |

> `Money` được map bằng `ComplexProperty` thành 2 cột `price_amount` + `price_currency`. `ProductName` map bằng `ComplexProperty` thành cột `name`.
> Hiện tại tính duy nhất của tên được kiểm tra ở tầng repository (`ExistsByNameAsync`); UNIQUE index ở mức DB là khuyến nghị bổ sung.
> ⚠️ Cột `code` là thiết kế đề xuất — **chưa có** trong `Product` entity hiện tại. Mã sinh tự động qua `ICodeGenerator.NextAsync("SP")` → `SP00001` (xem [code-generator-guide.md](code-generator-guide.md)).

---

# Cấu trúc Domain (DDD)

```
backend/src/MinimalAPI.Domain/
├── Entities/
│   ├── Product.cs                  # AggregateRoot<ProductId>, sealed, factory Create()
│   └── ProductId.cs                # readonly record struct ProductId(Guid Value)
├── ValueObjects/
│   ├── ProductName.cs              # sealed record, Create() — validate rỗng / max 200 ký tự
│   └── Money.cs                    # sealed record — Amount + Currency, Create()/VND()/Zero
├── Events/
│   ├── ProductCreatedEvent.cs      # raise khi tạo mới
│   └── ProductPriceChangedEvent.cs # raise khi đổi giá (kèm OldPrice, NewPrice)
└── Interfaces/
    └── IProductRepository.cs       # chỉ cho Aggregate Root
```

## Quy tắc Domain

- `Product` là **Aggregate Root**, kế thừa `AggregateRoot<ProductId>`, `sealed`.
- KHÔNG public setter — mọi thay đổi qua method (`Create`, `UpdateInfo`, `UpdatePrice`, `Activate`, `Deactivate`/`SetActive`).
- `ProductName`, `Money` là **Value Object** (sealed record) với factory `Create()` validate input.
- `ProductId`, `CategoryId` là **Typed ID** (`readonly record struct`) để type-safe.
- `Code` được sinh tự động (không cho người dùng tự nhập) qua `ICodeGenerator.NextAsync("SP")` trong Handler, truyền vào factory `Create()` — bất biến sau khi tạo.
- Raise `ProductCreatedEvent` khi tạo mới.
- Raise `ProductPriceChangedEvent` khi giá thay đổi (bỏ qua nếu giá không đổi).
- `UpdatePrice` / `SetActive` chỉ cập nhật `UpdatedAt` và raise event khi giá trị thực sự thay đổi.

---

# Quan hệ

```text
(Category) 1 — * (Product)
(Product)  1 — 1 (Inventory)
```

### Lưu ý

```text
- Mỗi Product thuộc đúng một Category (FK category_id, bắt buộc).
- Mỗi Product có tối đa một Inventory (xem inventory-guide.md).
- Khi xóa Product, Inventory tương ứng bị xóa theo (cascade).
- Money & ProductName là Value Object, không có Id, so sánh bằng giá trị.
```

---

# Thiết kế API

## Endpoints

| Method | Route                                  | Description                          |
|--------|----------------------------------------|--------------------------------------|
| GET    | `/api/products`                        | Danh sách (paged: `page`, `pageSize`, `search`) |
| GET    | `/api/products/{id}`                   | Chi tiết sản phẩm                    |
| GET    | `/api/products/code/{code}`            | Tra cứu theo mã (đề xuất, chưa có)   |
| POST   | `/api/products`                        | Tạo sản phẩm                         |
| PUT    | `/api/products/{id}`                   | Cập nhật sản phẩm                    |
| DELETE | `/api/products/{id}`                   | Xóa sản phẩm                         |
| GET    | `/api/products/by-category/{categoryId}` | Danh sách theo danh mục            |
| GET    | `/api/products/active`                 | Danh sách sản phẩm đang hoạt động    |
| GET    | `/api/products/deactive`               | Danh sách sản phẩm không hoạt động   |
| PUT    | `/api/products/{id}/active`            | Kích hoạt                            |
| PUT    | `/api/products/{id}/deactive`          | Vô hiệu hóa                          |

### Ví dụ — Tạo sản phẩm

```http
POST /api/products
```

```json
{
  "name": "Áo thun cotton",
  "price": 199000,
  "currency": "VND",
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Áo thun nam size M"
}
```

```json
{
  "isSuccess": true,
  "value": {
    "id": "8b1e...",
    "code": "SP00001",
    "name": "Áo thun cotton",
    "price": 199000,
    "currency": "VND",
    "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "categoryName": "Thời trang",
    "description": "Áo thun nam size M",
    "isActive": true,
    "createdAt": "2026-05-28T00:00:00Z"
  }
}
```

## Vertical Slices (Application Layer)

```
backend/src/MinimalAPI.Application/Features/Products/
├── CreateProduct/
│   ├── CreateProductCommand.cs
│   ├── CreateProductHandler.cs
│   └── CreateProductValidator.cs
├── UpdateProduct/
│   ├── UpdateProductCommand.cs
│   ├── UpdateProductHandler.cs
│   └── UpdateProductValidator.cs
├── DeleteProduct/
│   ├── DeleteProductCommand.cs
│   └── DeleteProductHandler.cs
├── GetProduct/
│   ├── GetProductQuery.cs
│   └── GetProductHandler.cs
├── GetProducts/
│   ├── GetProductsQuery.cs
│   └── GetProductsHandler.cs
├── GetProductsByCategory/
│   ├── GetProductsByCategoryQuery.cs
│   ├── GetProductsByCategoryHandler.cs
│   └── GetProductsByCategoryValidator.cs
├── GetProductActive/
│   ├── GetProductActiveQuery.cs
│   └── GetProductActiveHandler.cs
├── GetProductDeactive/
│   ├── GetProductDeactiveQuery.cs
│   └── GetProductDeactiveHandler.cs
├── UpdateProductActive/
│   ├── UpdateProductActiveCommand.cs
│   ├── UpdateProductActiveHandler.cs
│   └── UpdateProductActiveValidator.cs
├── UpdateProductDeactive/
│   ├── UpdateProductDeactiveCommand.cs
│   ├── UpdateProductDeactiveHandler.cs
│   └── UpdateProductDeactiveValidator.cs
└── DTOs/
    └── ProductDto.cs               # Id, Code, Name, Price, Currency, CategoryId, CategoryName, Description, IsActive, CreatedAt
```

Hạ tầng & API:

```
backend/src/MinimalAPI.Infrastructure/Persistence/
├── Configurations/ProductConfiguration.cs   # ToTable("products"), ComplexProperty cho Name & Money, HasConversion Typed ID
└── Repositories/ProductRepository.cs

backend/src/MinimalAPI.Api/Endpoints/ProductEndpoints.cs   # route group "/api/products" + WithTags("Products")
```

## Repository (`IProductRepository`)

Kế thừa `IRepository<Product, ProductId>`, bổ sung:

| Method                      | Mục đích                                   |
|-----------------------------|--------------------------------------------|
| `CountAsync`                | Đếm sản phẩm (phục vụ phân trang)         |
| `GetPagedAsync`             | Lấy trang sản phẩm theo `search`           |
| `GetByCategoryAsync`        | Lấy sản phẩm theo danh mục                 |
| `GetActiveProductsAsync`    | Lấy sản phẩm đang hoạt động                |
| `GetDeactiveProductsAsync`  | Lấy sản phẩm không hoạt động               |
| `ExistsByNameAsync`         | Kiểm tra tên sản phẩm đã tồn tại chưa      |

---

# Validation rules (FluentValidation)

| Field        | Rule                                                          |
|--------------|---------------------------------------------------------------|
| `Code`       | Không nhận từ client — sinh tự động `SP00001`, unique         |
| `Name`       | Required, tối đa 200 ký tự                                    |
| `Price`      | Required, `> 0`                                              |
| `Currency`   | Required, đúng 3 ký tự (VD `VND`, `USD`)                     |
| `CategoryId` | Required (`NotEmpty`)                                        |
| `Description`| Optional                                                     |

Kiểm tra nghiệp vụ / invariant bổ sung:

| Rule                          | Mô tả                                                        |
|-------------------------------|--------------------------------------------------------------|
| Tên không trùng               | `ExistsByNameAsync` ở repository                            |
| Category phải tồn tại         | Kiểm tra ở Handler trước khi tạo / cập nhật                 |
| `ProductName` hợp lệ          | VO `Create()` ném `DomainException` nếu rỗng / > 200 ký tự  |
| `Money` hợp lệ                | VO `Create()` ném `DomainException` nếu amount âm / currency ≠ 3 ký tự |

---

# Business Rules

- Product là Aggregate Root; mọi thay đổi trạng thái đi qua method của domain.
- Mỗi Product có mã `Code` duy nhất (`SP00001`...) sinh tự động qua `ICodeGenerator`, không cho người dùng tự nhập và không đổi sau khi tạo.
- Giá lưu dưới dạng `Money` (Amount + Currency) — không dùng số thuần.
- Đổi giá raise `ProductPriceChangedEvent`; tạo mới raise `ProductCreatedEvent`.
- Sản phẩm mới mặc định `IsActive = true`.
- Kích hoạt / vô hiệu hóa qua `Activate` / `Deactivate`; không đổi nếu trạng thái đã đúng.
- Mỗi Product gắn một Category bắt buộc và tối đa một Inventory.
- Xóa Product sẽ xóa Inventory tương ứng (cascade).
