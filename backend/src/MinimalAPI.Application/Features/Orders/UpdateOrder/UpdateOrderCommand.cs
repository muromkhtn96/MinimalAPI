using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;

namespace MinimalAPI.Application.Features.Orders.UpdateOrder;
/// <summary>
/// Lệnh cập nhật thông tin đơn hàng, bao gồm địa chỉ giao hàng, ghi chú và chi tiết đơn hàng.
/// </summary>
/// <param name="OrderId">ID của đơn hàng cần cập nhật</param>
/// <param name="Address">Địa chỉ giao hàng mới</param>
/// <param name="Note">Ghi chú mới cho đơn hàng (có thể null)</param>
/// <param name="Details">Danh sách chi tiết đơn hàng mới, bao gồm sản phẩm, số lượng và giá</param>
/// <returns>Kết quả trả về sau khi cập nhật đơn hàng, bao gồm thông tin đơn hàng đã được cập nhật</returns>
public sealed record UpdateOrderCommand(
    Guid OrderId,
    string Address,
    string? Note,
    List<OrderDetailDto> Details
) : IRequest<Result<OrderDto>>;