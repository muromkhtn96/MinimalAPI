using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.GetCustomers;

/// <summary>
/// Lấy danh sách khách hàng với phân trang và tìm kiếm.
/// </summary>
/// <param name="Keyword"></param>
/// <param name="Page"></param>
/// <param name="PageSize"></param>
/// <returns></returns>
public sealed record GetCustomersQuery(
    string? Keyword = null,
    int Page = 1, 
    int PageSize = 10)  
    : IRequest<Result<List<CustomersDto>>>; 