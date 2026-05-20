# Inventory Feature Guide

## Mục tiêu

Feature Inventory dùng để quản lý số lượng tồn kho hiện tại của sản phẩm.

### Chức năng

- Tạo tồn kho cho sản phẩm
- Xem tồn kho theo Id
- Cập nhật số lượng tồn kho
- Xóa tồn kho
- Đảm bảo mỗi sản phẩm chỉ có một dòng tồn kho

---

# Thiết kế Database

## Inventory

### Mục đích

Lưu số lượng tồn kho hiện tại của sản phẩm.

| Column | Type | Description |
|---|---|---|
| Id | bigint | Khóa chính |
| ProductId | bigint | FK -> Product |
| Quantity | long | Số lượng tồn hiện tại |
| CreatedAt | datetime | Ngày tạo |
| UpdatedAt | datetime | Ngày cập nhật |

---

# Quan hệ

```text
(Product) 1 — 1 (Inventory)
```

### Lưu ý

```text
Mỗi Product chỉ có duy nhất một Inventory record.
```

---

# Thiết kế API

## 1. Create Inventory

### Endpoint

```http
POST /api/inventories
```

### Request

```json
{
  "productId": 1,
  "quantity": 50
}
```

### Response

```json
{
  "success": true,
  "data": {
    "id": 1,
    "productId": 1,
    "quantity": 50
  }
}
```

---

## 2. Update Inventory

### Endpoint

```http
PUT /api/inventories/{id}
```

### Request

```json
{
  "quantity": 100
}
```

### Response

```json
{
  "success": true,
  "data": {
    "id": 1,
    "productId": 1,
    "quantity": 100
  }
}
```

---

## 3. Delete Inventory

### Endpoint

```http
DELETE /api/inventories/{id}
```

### Response

```json
{
  "success": true
}
```

---

## 4. GetById Inventory

### Endpoint

```http
GET /api/inventories/{id}
```

### Response

```json
{
  "success": true,
  "data": {
    "id": 1,
    "productId": 1,
    "quantity": 100
  }
}
```

---

# Cấu trúc VSA

```text
MinimalAPI.Api
└── Endpoints/
    └── InventoryEndpoints.cs

MinimalAPI.Application
└── Features/
    └── Inventories/
        ├── CreateInventory/
        │   ├── CreateInventoryCommand.cs
        │   ├── CreateInventoryHandler.cs
        │   └── CreateInventoryValidator.cs
        │
        ├── UpdateInventory/
        │   ├── UpdateInventoryCommand.cs
        │   ├── UpdateInventoryHandler.cs
        │   └── UpdateInventoryValidator.cs
        │
        ├── DeleteInventory/
        │   ├── DeleteInventoryCommand.cs
        │   ├── DeleteInventoryHandler.cs
        │   └── DeleteInventoryValidator.cs
        │
        ├── GetInventoryById/
        │   ├── GetInventoryByIdQuery.cs
        │   └── GetInventoryByIdHandler.cs
        │
        └── DTOs/
            └── InventoryDto.cs

MinimalAPI.Domain
└── Entities/
    └── Inventory.cs

└── Interfaces/
    └── IInventoryRepository.cs

MinimalAPI.Infrastructure
└── Persistence/
    └── Repositories/
        └── InventoryRepository.cs
```

---

# Validation Rules

|          Rule                    |           Description                       |
|----------------------------------|---------------------------------------------|
| ProductId bắt buộc               | ProductId is required                       |
| Quantity phải >= 0               | Quantity must be greater than or equal to 0 |
| Product phải tồn tại             | Product must exist                          |
| Mỗi Product chỉ có một Inventory | One inventory per product                   |

---

# Business Rules

- Inventory dùng để lưu số lượng tồn hiện tại của sản phẩm
- Mỗi Product chỉ được tạo một Inventory
- Không cho phép tạo nhiều Inventory cho cùng ProductId
- Update Inventory sẽ cập nhật Quantity hiện tại
- Delete Inventory sẽ xóa tồn kho của sản phẩm
- Khi Product bị xóa thì Inventory tương ứng cũng bị xóa