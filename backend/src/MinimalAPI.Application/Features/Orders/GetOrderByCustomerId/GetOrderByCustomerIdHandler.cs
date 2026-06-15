using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.GetOrderByCustomerId;

public sealed class GetOrderByCustomerIdHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    HybridCache hybridCache)
    : IRequestHandler<GetOrderByCustomerIdQuery, Result<List<OrderByCustomerDto>>>
{
    /// <summary>
    /// Xử lý truy vấn lấy danh sách đơn hàng của một khách hàng dựa trên ID
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<List<OrderByCustomerDto>>> Handle(GetOrderByCustomerIdQuery request, CancellationToken ct)
    {
        string cacheKey = $"orders:customer:{request.CustomerId}";
        
        var ordercustomerDtos = await hybridCache.GetOrCreateAsync<List<OrderByCustomerDto>?>(
            cacheKey,
            async token => 
            {
                var customerId = new CustomerId(request.CustomerId);
                
                var customer = await customerRepository.GetByIdAsync(customerId, token);
                if (customer is null)
                {
                    return null;
                }

                var orders = await orderRepository.GetByCustomerIdAsync(customerId, token);
                var orderByCustomerDtos = new List<OrderByCustomerDto>();
                
                foreach (var order in orders)
                {
                    var detailDtos = new List<OrderDetailByCustomerDto>();
                    foreach (var detail in order.Details)
                    {
                        var product = await productRepository.GetByIdAsync(detail.ProductId, token);
                        detailDtos.Add(new OrderDetailByCustomerDto(
                            detail.ProductId.Value,
                            product?.Name.Value ?? "Sản phẩm không xác định",
                            detail.Quantity
                        ));
                    }
                    
                    orderByCustomerDtos.Add(new OrderByCustomerDto(
                        order.Id.Value,
                        order.Code,
                        order.Status.ToString(),
                        order.TotalAmount.Amount,
                        order.CreatedAt,
                        detailDtos
                    ));
                }
                
                return orderByCustomerDtos;
            },
            cancellationToken: ct
        );

        if (ordercustomerDtos is null)
        {
            return Result<List<OrderByCustomerDto>>.Failure("Khách hàng không tồn tại");
        }

        return Result<List<OrderByCustomerDto>>.Success(ordercustomerDtos);
    }
}