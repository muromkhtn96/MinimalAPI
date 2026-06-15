using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;
/// <summary>
/// Lệnh tạo đơn hàng mới.
/// </summary>
/// <param name="CustomerId">ID của khách hàng tạo đơn hàng</param>
/// <param name="Note">Ghi chú cho đơn hàng</param>
/// <param name="Items">Danh sách các mặt hàng trong đơn hàng</param>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    string? Note,
    IReadOnlyList<CreateOrderItem> Items
) : IRequest<Result<OrderDto>>;

public sealed record CreateOrderItem(
    Guid ProductId,
    int Quantity
);