# Customer Feature Guide

## Mục tiêu

Feature Customer dùng để quản lý thông tin khách hàng (cá nhân & doanh nghiệp) trong hệ thống.

### Chức năng

- Tạo khách hàng mới
- Xem thông tin khách hàng theo Id
- Tìm kiếm / lọc danh sách khách hàng (theo tên, mã, loại, trạng thái)
- Cập nhật thông tin khách hàng
- Kích hoạt / vô hiệu hóa khách hàng
- Xóa khách hàng (soft hoặc hard delete)

---

# Thiết kế Database

## Customer

### Mục đích

Lưu thông tin khách hàng — bao gồm thông tin cá nhân, liên hệ, địa chỉ và trạng thái.

### Bảng `customers`

| Column            | Type           | Nullable | Description                                  |
|-------------------|----------------|----------|----------------------------------------------|
| id                | uuid           | NO       | Khóa chính (CustomerId — Typed ID)           |
| code              | varchar(20)    | NO       | Mã khách hàng (unique, VD: `KH0001`)         |
| full_name         | varchar(200)   | NO       | Họ và tên / Tên doanh nghiệp                 |
| email             | varchar(256)   | YES      | Email (unique nếu có) — Value Object         |
| phone             | varchar(20)    | YES      | Số điện thoại — Value Object                 |
| type              | smallint       | NO       | Loại khách hàng (0: Individual, 1: Company)  |
| tax_code          | varchar(20)    | YES      | Mã số thuế (chỉ áp dụng với Company)         |
| gender            | smallint       | YES      | Giới tính (0: Male, 1: Female, 2: Other)     |
| date_of_birth     | date           | YES      | Ngày sinh                                    |
| address_street    | varchar(200)   | YES      | Địa chỉ — số nhà, đường (VO `Address`)       |
| address_ward      | varchar(100)   | YES      | Phường / Xã                                  |
| address_district  | varchar(100)   | YES      | Quận / Huyện                                 |
| address_city      | varchar(100)   | YES      | Tỉnh / Thành phố                             |
| address_country   | varchar(100)   | YES      | Quốc gia (default: `Vietnam`)                |
| note              | text           | YES      | Ghi chú nội bộ                               |
| is_active         | boolean        | NO       | Trạng thái hoạt động (default: `true`)       |
| created_at        | timestamptz    | NO       | Ngày tạo (UTC)                               |
| updated_at        | timestamptz    | YES      | Ngày cập nhật gần nhất (UTC)                 |

### Index đề xuất

| Index                       | Columns       | Loại    | Mục đích                              |
|-----------------------------|---------------|---------|---------------------------------------|
| `ux_customers_code`         | `code`        | UNIQUE  | Tra cứu nhanh theo mã, đảm bảo unique |
| `ux_customers_email`        | `email`       | UNIQUE  | Đảm bảo email không trùng (nếu có)    |
| `ix_customers_phone`        | `phone`       | BTREE   | Tìm kiếm theo số điện thoại           |
| `ix_customers_full_name`    | `full_name`   | BTREE   | Tìm kiếm theo tên                     |
| `ix_customers_is_active`    | `is_active`   | BTREE   | Lọc khách hàng active                 |

---

# Cấu trúc Domain (DDD)

```
backend/src/MinimalAPI.Domain/
├── Entities/
│   ├── Customer.cs                 # AggregateRoot<CustomerId>, sealed, factory Create()
│   └── CustomerId.cs               # readonly record struct CustomerId(Guid Value)
├── ValueObjects/
│   ├── Email.cs                    # sealed record, Create() — validate format
│   ├── PhoneNumber.cs              # sealed record, Create() — validate format VN
│   └── Address.cs                  # sealed record — street, ward, district, city, country
├── Enums/
│   ├── CustomerType.cs             # Individual = 0, Company = 1
│   └── Gender.cs                   # Male = 0, Female = 1, Other = 2
├── Events/
│   ├── CustomerCreatedEvent.cs
│   └── CustomerDeactivatedEvent.cs
└── Interfaces/
    └── ICustomerRepository.cs      # chỉ cho Aggregate Root
```

## Quy tắc Domain

- `Customer` là **Aggregate Root**, kế thừa `AggregateRoot<CustomerId>`.
- KHÔNG public setter — mọi thay đổi qua method (`Create`, `UpdateInfo`, `ChangeAddress`, `Activate`, `Deactivate`).
- `Email`, `PhoneNumber`, `Address` là **Value Object** (sealed record) với factory `Create()` validate input.
- `CustomerId` là **Typed ID** (`readonly record struct`) để type-safe.
- Raise `CustomerCreatedEvent` khi tạo mới.

---

# Quan hệ

```text
(Customer) 1 — * (Order)        [nếu có module Order]
(Customer) 1 — * (Invoice)      [nếu có module Invoice]
```

### Lưu ý

```text
- Customer là Aggregate Root độc lập — không phụ thuộc entity nào khác.
- Email & Phone là Value Object nhưng vẫn unique ở mức DB (qua index).
- Soft delete khuyến nghị: dùng cờ is_active thay vì xóa cứng, để giữ lịch sử đơn hàng.
```

---

# Thiết kế API

## Endpoints

| Method | Route                          | Description                          |
|--------|--------------------------------|--------------------------------------|
| POST   | `/api/customers`               | Tạo khách hàng                       |
| GET    | `/api/customers`               | Danh sách (paged, filter, search)    |
| GET    | `/api/customers/{id}`          | Chi tiết khách hàng                  |
| GET    | `/api/customers/code/{code}`   | Tra cứu theo mã                      |
| PUT    | `/api/customers/{id}`          | Cập nhật thông tin                   |
| PATCH  | `/api/customers/{id}/activate` | Kích hoạt                            |
| PATCH  | `/api/customers/{id}/deactivate` | Vô hiệu hóa                        |
| DELETE | `/api/customers/{id}`          | Xóa                                  |

## Vertical Slices (Application Layer)

```
backend/src/MinimalAPI.Application/Features/Customers/
├── CreateCustomer/
│   ├── CreateCustomerCommand.cs
│   ├── CreateCustomerHandler.cs
│   └── CreateCustomerValidator.cs
├── UpdateCustomer/
├── DeleteCustomer/
├── ActivateCustomer/
├── DeactivateCustomer/
├── GetCustomer/
├── GetCustomers/
├── GetCustomerByCode/
└── DTOs/
    └── CustomerDto.cs
```

## Validation rules (FluentValidation)

| Field       | Rule                                                              |
|-------------|-------------------------------------------------------------------|
| `Code`      | Required, 3–20 ký tự, unique                                      |
| `FullName`  | Required, 2–200 ký tự                                             |
| `Email`     | Optional, đúng định dạng email, unique nếu có                     |
| `Phone`     | Optional, đúng định dạng SĐT VN (10–11 số, bắt đầu bằng 0 hoặc +84) |
| `Type`      | Required, thuộc enum `CustomerType`                               |
| `TaxCode`   | Required nếu `Type = Company`, 10–13 số                           |

---

# Câu hỏi cần làm rõ trước khi implement

1. **Email & Phone**: dùng Value Object (có validate ở domain) hay string thường?
2. **Phân biệt cá nhân / doanh nghiệp**: có cần field `type` + `tax_code` không, hay chỉ lưu cá nhân?
3. **Địa chỉ**: 1 địa chỉ duy nhất (VO inline) hay nhiều địa chỉ (entity riêng `CustomerAddress`)?
4. **Mã `code`**: tự sinh theo sequence (VD `KH0001`) hay người dùng tự nhập?
5. **Xóa**: soft delete (chỉ set `is_active = false`) hay hard delete?
