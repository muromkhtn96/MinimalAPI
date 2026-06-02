namespace MinimalAPI.Application.Features.Customers.DTOs;

/// <summary>
/// DTO dùng để trả về danh sách khách hàng, chỉ chứa các thông tin cơ bản như Id, Code, FullName và IsActive.
/// </summary>
/// <param name="id"></param>
/// <param name="Code"></param>
/// <param name="FullName"></param>
/// <param name="IsActive"></param>
/// <returns></returns>
public record CustomersDto(
    Guid id,
    string Code,
    string FullName,
    bool IsActive
);