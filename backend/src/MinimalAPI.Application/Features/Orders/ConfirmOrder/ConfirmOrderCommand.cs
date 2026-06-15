using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.ConfirmOrder;
/// <summary>
/// Lệnh xác nhận đơn hàng
/// </summary>
/// <param name="OrderId">ID của đơn hàng cần xác nhận</param>
/// <returns></returns>
public sealed record ConfirmOrderCommand(
    /// <summary> Mã đơn hàng cần xác nhận. </summary>
    Guid Id)  : IRequest<Result<OrderDto>>;