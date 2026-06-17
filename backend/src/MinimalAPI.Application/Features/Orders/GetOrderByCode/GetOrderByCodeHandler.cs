using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.GetOrderByCode;

public sealed class GetOrderByCodeHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    ICacheService cacheService) 
    : IRequestHandler<GetOrderByCodeQuery, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý truy vấn lấy thông tin đơn hàng theo mã đơn hàng.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<OrderDto>> Handle(GetOrderByCodeQuery request, CancellationToken ct)
    {
        string cacheKey = CacheKeys.OrderByCode(request.Code);

        var dto = await cacheService.GetOrCreateAsync<OrderDto?>(
            cacheKey,
            async token =>
            {
                var order = await orderRepository.GetByCodeAsync(request.Code, token);
                if (order is null)
                {
                    return null;
                }

                var customer = await customerRepository.GetByIdAsync(order.CustomerId, token);
                var customerName = customer?.FullName ?? "Khách vô danh";

                var detailDtos = new List<OrderDetailDto>();
                foreach (var detail in order.Details)
                {
                    var product = await productRepository.GetByIdAsync(detail.ProductId, token);
                    detailDtos.Add(new OrderDetailDto(
                        detail.ProductId.Value,
                        product?.Name.Value ?? "Sản phẩm không xác định",
                        detail.Quantity,
                        detail.UnitPrice.Amount,
                        detail.LineTotal.Amount));
                }

                return new OrderDto(
                    order.Id.Value,
                    order.Code,
                    order.CustomerId.Value,
                    customerName,
                    order.Status.ToString(),
                    order.TotalAmount.Amount,
                    order.TotalAmount.Currency,
                    order.Note,
                    order.CreatedAt,
                    detailDtos);
            },
            cancellationToken: ct);
        
        if (dto is null)
        {
            return Result<OrderDto>.Failure("Không tìm thấy đơn hàng.");
        }

        return Result<OrderDto>.Success(dto);
    }
}