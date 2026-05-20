# EF Core Migrations Guide

Hướng dẫn chạy migration với EF Core ở local machine (Windows + PowerShell).

---

## Yêu cầu trước khi chạy

1. **Postgres container đang chạy**
   ```powershell
   docker ps --filter "name=PostgresDB"
   ```
   Nếu chưa: `docker compose -f docker-compose.dev.yml up -d db`

2. **EF Core tools đã cài**
   ```powershell
   dotnet ef --version
   # Nếu chưa cài:
   dotnet tool install --global dotnet-ef
   # Hoặc update:
   dotnet tool update --global dotnet-ef
   ```

3. **Connection string khớp DB**
   - Đứng ở local: dùng [appsettings.Development.json](../backend/src/MinimalAPI.Api/appsettings.Development.json)
   - Host phải là `localhost` (không phải `db`), password phải khớp với `POSTGRES_PASSWORD` trong [docker-compose.dev.yml](../docker-compose.dev.yml)

---

## Lệnh cơ bản

### Đứng ở root project (`d:\Projects\MinimalAPI`)

```powershell
dotnet ef database update `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api
```

### Đứng ở thư mục `backend`

```powershell
cd backend

dotnet ef database update `
    --project src/MinimalAPI.Infrastructure `
    --startup-project src/MinimalAPI.Api
```

### Giải thích flag

| Flag                 | Ý nghĩa                                                          |
|----------------------|------------------------------------------------------------------|
| `--project`          | Project chứa **DbContext** + migrations (Infrastructure)         |
| `--startup-project`  | Project chứa **appsettings + DI** để EF biết connection string (Api) |

> Không có 2 flag này, EF không biết lấy connection string ở đâu → fail.

---

## Các lệnh thường dùng

### Tạo migration mới

```powershell
dotnet ef migrations add <TenMigration> `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api
```

> **Convention đặt tên**: dùng PascalCase, mô tả ngắn gọn thay đổi.
> Ví dụ: `AddCustomerTable`, `AddIndexOnProductName`, `RenameColumnEmailToContactEmail`.

### Apply tất cả migration pending

```powershell
dotnet ef database update `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api
```

### Xem danh sách migration (đã apply / chưa apply)

```powershell
dotnet ef migrations list `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api
```

Output dạng:
```
20260407075615_InitialCreate (Applied)
20260424101530_SyncModelAfterChanges (Applied)
20260520073941_AddCodeCounters (Applied)
20260601120000_AddCustomer (Pending)
```

### Rollback về migration cụ thể

```powershell
# Rollback về migration AddCodeCounters (vô hiệu các migration sau nó)
dotnet ef database update AddCodeCounters `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api
```

> Có thể dùng tên đầy đủ hoặc tên rút gọn (phần sau timestamp).

### Rollback toàn bộ về trạng thái rỗng

```powershell
dotnet ef database update 0 `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api
```

⚠️ **Nguy hiểm** — sẽ drop hết bảng. Chỉ dùng khi reset dev environment.

### Xóa migration chưa apply

```powershell
# Chỉ xóa file migration chưa được apply lên DB
dotnet ef migrations remove `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api
```

> Nếu migration đã apply rồi mà muốn xóa: phải rollback trước (xem mục trên), rồi mới chạy `remove`.

### Generate SQL script (không apply trực tiếp)

```powershell
# Sinh SQL từ rỗng → migration mới nhất
dotnet ef migrations script `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api `
    --output migration.sql

# Sinh SQL từ migration cụ thể → mới nhất
dotnet ef migrations script AddCodeCounters `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api `
    --output migration.sql

# Idempotent script (có thể chạy lại nhiều lần, chỉ apply phần chưa có)
dotnet ef migrations script --idempotent `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api `
    --output migration.sql
```

> Hữu ích khi deploy lên production — DBA review SQL trước khi chạy.

---

## Workflow điển hình khi thêm entity mới

1. **Sửa code** — thêm entity, EF configuration, DbSet
2. **Build verify** — `dotnet build backend/MinimalAPI.slnx`
3. **Tạo migration**:
   ```powershell
   dotnet ef migrations add AddCustomerTable `
       --project backend/src/MinimalAPI.Infrastructure `
       --startup-project backend/src/MinimalAPI.Api
   ```
4. **Review file migration** — mở file `*_AddCustomerTable.cs` trong [Migrations/](../backend/src/MinimalAPI.Infrastructure/Migrations) kiểm tra SQL được sinh ra có đúng không
5. **Apply lên DB**:
   ```powershell
   dotnet ef database update `
       --project backend/src/MinimalAPI.Infrastructure `
       --startup-project backend/src/MinimalAPI.Api
   ```
6. **Verify**:
   ```powershell
   docker exec PostgresDB psql -U postgres -d minimalapi_dev -c "\dt"
   ```

---

## Troubleshooting

### `password authentication failed for user "postgres"`

Password trong `appsettings.Development.json` không khớp với DB.

**Kiểm tra**:
```powershell
# Password trong DB (xem env var docker-compose.dev.yml block `db`):
Select-String -Path docker-compose.dev.yml -Pattern "POSTGRES_PASSWORD"

# Password trong appsettings:
Select-String -Path backend/src/MinimalAPI.Api/appsettings.Development.json -Pattern "Password"
```

**Fix**: sửa 1 trong 2 để khớp nhau. Lưu ý: env var `POSTGRES_PASSWORD` chỉ có tác dụng khi volume DB lần đầu init; nếu DB đã chạy rồi thì phải ALTER USER:

```powershell
docker exec PostgresDB psql -U postgres -d minimalapi_dev `
    -c "ALTER USER postgres WITH PASSWORD 'postgres123';"
```

### `Unable to create a 'DbContext' of type 'AppDbContext'`

Thường do startup project thiếu `IConfiguration` setup hoặc `AddDbContext` không đăng ký. Đảm bảo `--startup-project` trỏ đúng vào project Api.

### `The Entity Framework tools version '...' is older than that of the runtime '...'`

Cập nhật EF tools global:
```powershell
dotnet tool update --global dotnet-ef
```

### Override connection string tạm thời (không sửa file)

Khi cần test nhanh với connection string khác:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=minimalapi_dev;Username=postgres;Password=postgres123"

dotnet ef database update `
    --project backend/src/MinimalAPI.Infrastructure `
    --startup-project backend/src/MinimalAPI.Api

# Xóa env var sau khi xong:
Remove-Item Env:\ConnectionStrings__DefaultConnection
```
