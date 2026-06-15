using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.GetOrderById;
/// <summary>
/// Truy vấn lấy đơn hàng theo ID
/// </summary>
/// <param name="id"></param>
public sealed record GetOrderByIdQuery(Guid id) : IRequest<Result<OrderDto>>;