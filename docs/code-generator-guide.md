# Code Generator Helper Guide

## Mục tiêu

Helper `ICodeGenerator` dùng để sinh **mã code tuần tự** theo prefix cho các entity cần mã hiển thị cho người dùng (khách hàng, đơn hàng, sản phẩm, hóa đơn...).

### Tính chất

- **Generic theo prefix**: Một helper duy nhất, dùng được cho mọi entity (`KH`, `DH`, `SP`, `HD`...).
- **An toàn dưới concurrency**: Dùng atomic UPSERT của PostgreSQL — không cần lock thủ công.
- **Auto-init**: Prefix mới lần đầu gọi tự tạo counter, không cần seed.
- **Định dạng linh hoạt**: Số chữ số phần số có thể tùy chỉnh (default 5 → `KH00001`).

---

## Cấu trúc

```
backend/src/
├── MinimalAPI.Application/
│   └── Abstractions/
│       └── ICodeGenerator.cs              # Interface
└── MinimalAPI.Infrastructure/
    ├── Persistence/
    │   ├── CodeCounter.cs                 # Entity counter (internal)
    │   └── Configurations/
    │       └── CodeCounterConfiguration.cs
    └── Services/
        └── CodeGenerator.cs               # Implementation
```

### Bảng `code_counters`

| Column         | Type         | Description                              |
|----------------|--------------|------------------------------------------|
| prefix         | varchar(10)  | PK — tiền tố (VD: `KH`, `DH`, `SP`)      |
| current_value  | bigint       | Giá trị counter hiện tại                 |

---

## API

```csharp
public interface ICodeGenerator
{
    Task<string> NextAsync(
        string prefix,
        int padLength = 5,
        CancellationToken ct = default);
}
```

### Tham số

| Param        | Mô tả                                                                | Mặc định |
|--------------|----------------------------------------------------------------------|----------|
| `prefix`     | Tiền tố mã. Chỉ chấp nhận **A–Z**, độ dài 1–10 ký tự. Tự uppercase.  | (bắt buộc) |
| `padLength`  | Số chữ số phần số (1–20). VD `5` → `KH00001`, `6` → `KH000001`.      | `5`      |
| `ct`         | Cancellation token.                                                  | `default`|

### Output

- Định dạng: `{PREFIX}{NUMBER:PadLeft(padLength,'0')}`
- Ví dụ: `KH00001`, `DH00042`, `SP01234`

### Ngoại lệ

| Tình huống                                     | Exception                       |
|------------------------------------------------|---------------------------------|
| `prefix` rỗng / whitespace                     | `ArgumentException`             |
| `prefix` chứa ký tự không phải A-Z             | `ArgumentException`             |
| `prefix` dài quá 10 ký tự                      | `ArgumentException`             |
| `padLength` ngoài [1, 20]                      | `ArgumentOutOfRangeException`   |

---

## Cách dùng

### 1. Tiêm vào Handler

```csharp
public sealed class CreateCustomerHandler(
    ICustomerRepository repo,
    IUnitOfWork uow,
    ICodeGenerator codeGenerator)
    : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCustomerCommand cmd,
        CancellationToken ct)
    {
        // Sinh mã khách hàng: KH00001, KH00002, ...
        var code = await codeGenerator.NextAsync("KH", ct: ct);

        var customer = Customer.Create(code, cmd.FullName, /* ... */);
        repo.Add(customer);
        await uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(customer.Id.Value);
    }
}
```

### 2. Quy ước prefix đề xuất

| Entity        | Prefix | Ví dụ      |
|---------------|--------|------------|
| Khách hàng    | `KH`   | `KH00001`  |
| Đơn hàng      | `DH`   | `DH00001`  |
| Sản phẩm      | `SP`   | `SP00001`  |
| Hóa đơn       | `HD`   | `HD00001`  |
| Phiếu nhập    | `PN`   | `PN00001`  |
| Phiếu xuất    | `PX`   | `PX00001`  |

> Không bắt buộc — bạn có thể đặt prefix tùy ý miễn là A-Z và ≤10 ký tự.

### 3. Khi cần `padLength` lớn hơn

Nếu dự kiến entity có > 99,999 records → tăng `padLength`:

```csharp
// Đơn hàng có thể vượt 100k → dùng 7 chữ số
var code = await codeGenerator.NextAsync("DH", padLength: 7, ct: ct);
// → DH0000001
```

⚠️ **Lưu ý**: Đừng đổi `padLength` giữa chừng cho cùng prefix → sẽ tạo ra mã không thống nhất độ dài (VD: `KH00001` và `KH000001` cùng tồn tại).

---

## Hành vi trong Transaction

### Counter rollback theo transaction

```csharp
await uow.BeginTransactionAsync(ct);
try
{
    var code = await codeGenerator.NextAsync("KH", ct: ct); // KH00005
    // ... logic khác lỗi
    await uow.RollbackAsync(ct);
    // → counter rollback về 4, lần gọi sau lại lấy KH00005
}
catch { ... }
```

✅ **Counter và business logic cùng atomic** — nếu transaction rollback, counter cũng rollback.

### Gap-free KHÔNG được đảm bảo

Nếu transaction rollback rồi nhưng đồng thời có request khác đã `NextAsync` thành công → có thể có **gap**:

```
T1: NextAsync("KH") → KH00005 (chưa commit)
T2: NextAsync("KH") → KH00006 (chưa commit, chờ T1)
T1: rollback        → counter về 4? Không! Vì T2 đã update lên 6.
T2: commit          → KH00006 tồn tại, KH00005 không bao giờ tồn tại.
```

⚠️ Nếu nghiệp vụ yêu cầu mã **liên tục không bỏ sót** (VD: số hóa đơn pháp lý theo Nghị định 123) → cần thiết kế khác (sinh mã trong stored procedure sau khi insert thành công, hoặc dùng số riêng cho hóa đơn).

---

## Migration

Khi thêm `CodeCounter` lần đầu, cần tạo migration để DB có bảng `code_counters`:

```powershell
cd backend
dotnet ef migrations add AddCodeCounters `
    --project src/MinimalAPI.Infrastructure `
    --startup-project src/MinimalAPI.Api

dotnet ef database update `
    --project src/MinimalAPI.Infrastructure `
    --startup-project src/MinimalAPI.Api
```

---

## Test thử

```csharp
[Fact]
public async Task NextAsync_FirstCall_ReturnsPaddedCode()
{
    var code = await _codeGenerator.NextAsync("TEST");
    Assert.Equal("TEST00001", code);
}

[Fact]
public async Task NextAsync_MultipleCalls_IncrementsSequentially()
{
    var c1 = await _codeGenerator.NextAsync("TEST");
    var c2 = await _codeGenerator.NextAsync("TEST");
    var c3 = await _codeGenerator.NextAsync("TEST");

    Assert.Equal("TEST00001", c1);
    Assert.Equal("TEST00002", c2);
    Assert.Equal("TEST00003", c3);
}

[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData("kh-01")]      // chứa '-' và số
[InlineData("ABCDEFGHIJK")] // 11 ký tự
public async Task NextAsync_InvalidPrefix_Throws(string prefix)
{
    await Assert.ThrowsAsync<ArgumentException>(
        () => _codeGenerator.NextAsync(prefix));
}
```

---

## Khi nào KHÔNG nên dùng helper này

- **Mã cần unpredictable** (VD: token chia sẻ, mã giảm giá): Dùng `Guid.NewGuid().ToString("N")[..8]` thay vì counter.
- **Mã cần encode thông tin** (VD: `DH-2025-001` theo năm): Cần helper riêng tự xử lý reset theo năm.
- **Hóa đơn pháp lý** cần gap-free liên tục: Xem mục [Gap-free KHÔNG được đảm bảo](#gap-free-không-được-đảm-bảo).
