using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.GetOrderByCode;
/// <summary>
/// Truy vấn lấy đơn hàng theo mã đơn hàng
/// </summary>
/// <param name="Code"></param>
public sealed record GetOrderByCodeQuery(string Code)
    : IRequest<Result<OrderDto>>;