using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.GetOrders;

public sealed class GetOrdersHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrdersQuery, Result<List<OrdersDto>>>
{
    /// <summary>
    /// Xử lý truy vấn lấy danh sách đơn hàng
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<List<OrdersDto>>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var orders = await orderRepository.GetPageAsync(
            request.Page,
            request.PageSize,
            request.Search,
            ct);

        var orderDtos = orders
            .Select(order => new OrdersDto(
            order.Id.Value,
            order.Code,
            order.CustomerId.Value,
            order.Status.ToString()))
            .ToList();

        return Result<List<OrdersDto>>.Success(orderDtos);
    }
}