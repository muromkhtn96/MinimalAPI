using MinimalAPI.Domain.Enums;
using MinimalAPI.Domain.Events;
using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

/// <summary>
/// Đại diện cho thông tin khách hàng trong hệ thống.
/// </summary>
public sealed class Customer : AggregateRoot<CustomerId>
{
    /// <summary>
    /// Lấy mã khách hàng.
    /// </summary>
    /// <value></value>
    public string Code { get; private set; } = default!;
    /// <summary>
    /// Lấy họ và tên đầy đủ.
    /// </summary>
    /// <value></value>
    public string FullName { get; private set; } = default!;
    /// <summary>
    /// Lấy email.
    /// </summary>
    /// <value></value>
    public string? Email { get; private set; }
    /// <summary>
    /// Lấy số điện thoại.
    /// </summary>
    /// <value></value>
    public string? Phone { get; private set; }
    /// <summary>
    /// Lấy địa chỉ.
    /// </summary>
    /// <value></value>
    public string? Address { get; private set; }
    /// <summary>
    /// Lấy kiểu khách hàng.
    /// </summary>
    /// <value></value>
    public CustomerType Type { get; private set; }
    /// <summary>
    /// Lấy mã số thuế.
    /// </summary>
    /// <value></value>
    public string? TaxCode { get; private set; }
    /// <summary>
    /// Lấy giới tính.
    /// </summary>
    /// <value></value>
    public Gender? Gender { get; private set; }
    /// <summary>
    /// Lấy ngày sinh.
    /// </summary>
    /// <value></value>
    public DateTime? DateOfBirth { get; private set; }
    /// <summary>
    /// Lấy ghi chú.
    /// </summary>
    /// <value></value>
    public string? Note { get; private set; }
    /// <summary>
    /// Lấy trạng thái hoạt động.
    /// </summary>
    /// <value></value> 
    public bool IsActive { get; private set; }
    /// <summary>
    /// Lấy ngày tạo
    /// </summary> <summary>
    /// </summary>
    /// <value></value>
    public DateTime CreatedAt { get; private set; }


    /// <summary>
    /// Ngày cập nhật
    /// </summary>
    /// <value></value>
    public DateTime? UpdateAt { get; private set; }

    /// <summary>
    /// Hàm khởi tạo mặc định cho ORM hoặc khi cần tạo đối tượng rỗng.
    /// </summary>
    private Customer() { }

    /// <summary>
    /// Tạo mới khách hàng.
    /// </summary>
    public static Customer Create(
        string code,
        string fullName,
        string? email,
        string? phone,
        CustomerType type,
        string? taxCode,
        Gender? gender,
        DateTime? dateOfBirth,
        string? address,
        string? note)
    {
        var customer = new Customer
        {
            Id = CustomerId.New(),
            Code = code,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Type = type,
            TaxCode = taxCode,
            Gender = gender,
            DateOfBirth = dateOfBirth,
            Address = address,
            Note = note,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        customer.RaiseDomainEvent(new CustomerCreatedEvent(customer.Id));

        return customer;
    }

    /// <summary>
    /// Cập nhật thông tin khách hàng.
    /// </summary>
    public void UpdateInfo(
        string fullName,
        string? phone,
        string? taxCode,
        Gender? gender,
        DateTime? dateOfBirth,
        string? address,
        string? note)
    {
        FullName = fullName;
        Phone = phone;
        TaxCode = taxCode;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Address = address;
        Note = note;
        UpdateAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Thay đổi trạng thái hoạt động của khách hàng.
    /// </summary>
    public void SetActive(bool isActive)
    {
        if (IsActive == isActive) return;

        IsActive = isActive;
        UpdateAt = DateTime.UtcNow;

        if (!isActive)
        {
            RaiseDomainEvent(new CustomerDeactivatedEvent(Id));
        }
    }

    /// <summary>
    /// Vô hiệu hóa khách hàng.
    /// </summary>
    public void Deactivate() => SetActive(false);

    /// <summary>
    /// Kích hoạt lại khách hàng.
    /// </summary>
    public void Activate() => SetActive(true);
}