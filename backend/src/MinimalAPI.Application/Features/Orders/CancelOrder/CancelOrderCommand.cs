using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.CancelOrder;
/// <summary>
/// Lệnh hủy đơn hàng
/// </summary>
/// <param name="OrderId">ID của đơn hàng cần hủy</param>
/// <returns></returns>
public sealed record CancelOrderCommand(
    /// <summary> Mã đơn hàng cần đóng. </summary>
    Guid Id) : IRequest<Result<OrderDto>>;