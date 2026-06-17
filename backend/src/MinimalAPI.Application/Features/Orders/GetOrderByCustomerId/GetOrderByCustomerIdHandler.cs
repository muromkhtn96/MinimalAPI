using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.GetOrderByCustomerId;

public sealed class GetOrderByCustomerIdHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository)
    : IRequestHandler<GetOrderByCustomerIdQuery, Result<List<OrderByCustomerDto>>>
{
    public async Task<Result<List<OrderByCustomerDto>>> Handle(GetOrderByCustomerIdQuery request, CancellationToken ct)
    {
        var customerId = new CustomerId(request.CustomerId);

        var customer = await customerRepository.GetByIdAsync(customerId, ct);
        if (customer is null)
        {
            return Result<List<OrderByCustomerDto>>.Failure("Khách hàng không tồn tại");
        }

        var orders = await orderRepository.GetByCustomerIdAsync(customerId, ct);

        var orderByCustomerDtos = new List<OrderByCustomerDto>();

        foreach (var order in orders)
        {
            var detailDtos = new List<OrderDetailByCustomerDto>();

            foreach (var detail in order.Details)
            {
                var product = await productRepository.GetByIdAsync(detail.ProductId, ct);

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

        return Result<List<OrderByCustomerDto>>.Success(orderByCustomerDtos);
    }
}