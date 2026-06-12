using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.GetOrderByCustomerId;

/// <summary>
/// Truy vấn lấy danh sách đơn hàng của một khách hàng dựa trên ID khách hàng
/// </summary>
/// <param name="CustomerId">ID của khách hàng</param>
/// <returns></returns>
public sealed record GetOrderByCustomerIdQuery(Guid CustomerId) : IRequest<Result<List<OrderByCustomerDto>>>;