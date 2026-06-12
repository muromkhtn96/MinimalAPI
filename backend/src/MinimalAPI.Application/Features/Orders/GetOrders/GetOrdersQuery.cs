using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.GetOrders;
/// <summary>
/// Lấy danh sách đơn hàng với phân trang và tìm kiếm
/// </summary>
/// <param name="Page"></param>
/// <param name="PageSize"></param>
/// <param name="Search"></param>
/// <returns></returns>
public sealed record GetOrdersQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null) 
    : IRequest<Result<List<OrdersDto>>>;