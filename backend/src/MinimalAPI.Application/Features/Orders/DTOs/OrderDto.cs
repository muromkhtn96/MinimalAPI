namespace MinimalAPI.Application.Features.Orders.DTOs;
/// <summary>
/// Đại diện cho dữ liệu đơn hàng
/// </summary>
/// <param name="Id"></param>
/// <param name="Code"></param>
/// <param name="CustomerId"></param>
/// <param name="CustomerName"></param>
/// <param name="Status"></param>
/// <param name="TotalAmount"></param>
/// <param name="Currency"></param>
/// <param name="Note"></param>
/// <param name="CreatedAt"></param>
/// <param name="Details"></param>
/// <returns></returns>
public record OrderDto(
    Guid Id,
    string Code,
    Guid CustomerId,
    string CustomerName,          
    string Status,            
    decimal TotalAmount,      
    string Currency,         
    string? Note,
    DateTime CreatedAt,
    List<OrderDetailDto> Details
);
/// <summary>
/// Đại diện cho dữ liệu chi tiết đơn hàng
/// </summary>
/// <param name="ProductId"></param>
/// <param name="ProductName"></param>
/// <param name="Quantity"></param>
/// <param name="UnitPrice"></param>
/// <param name="LineTotal"></param>
/// <returns></returns>
public record OrderDetailDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
/// <summary>
/// Đại diện cho danh sách đơn hàng
/// </summary>
/// <param name="Id"></param>
/// <param name="Code"></param>
/// <param name="CustomerId"></param>
/// <param name="Status"></param>
/// <returns></returns>
public record OrdersDto(
    Guid Id,
    string Code,
    Guid CustomerId,
    string Status
);

public record OrderByCustomerDto(
    Guid Id,
    string Code,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    List<OrderDetailByCustomerDto> DetailByCustomerDtos
);
public record OrderDetailByCustomerDto(
    Guid ProductId,
    string ProductName,
    int Quantity
);