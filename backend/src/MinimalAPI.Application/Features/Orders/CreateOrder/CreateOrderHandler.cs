using MediatR;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Abstractions.Messaging;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Application.IntegrationEvents.Orders;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Enums;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo,
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IUnitOfWorkManager unitOfWorkManager,
    // HybridCache hybridCache,
    ICodeGenerator codeGenerator,
    IIntegrationEventPublisher eventPublisher,
    ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý lệnh tạo đơn hàng mới, bao gồm xác thực khách hàng, kiểm tra tồn kho và ghi dữ liệu trong Unit of Work
    /// </summary>
    /// <param name="Products"></param>
    /// <param name="Details"></param>
    private record CreatePreparedData(List<Product> Products, List<OrderDetail> Details);
    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var customerResult = await GetAndValidateCustomerAsync(request.CustomerId, ct);
        if (!customerResult.IsSuccess)
            return Result<OrderDto>.Failure(customerResult.Error!);
        var customer = customerResult.Value!;
        var orderId = OrderId.New();

        var prepareResult = await PrepareOrderDetailsAndVerifyInventoryAsync(orderId, request.Items, ct);
        if (!prepareResult.IsSuccess)
            return Result<OrderDto>.Failure(prepareResult.Error!);
        var preparedData = prepareResult.Value!;
        if (preparedData.Details.Count == 0)
        {
            return Result<OrderDto>.Failure("Đơn hàng phải có ít nhất một sản phẩm.");
        }

        var code = await codeGenerator.NextAsync("DH", 5, ct);

        var order = new Order(orderId)
        {
            Code = code,
            CustomerId = customer.Id,
            Status = OrderStatus.Pending,
            Note = request.Note?.Trim(),
            CreatedAt = DateTime.UtcNow,
            Details = preparedData.Details,
            TotalAmount = preparedData.Details.Aggregate(Money.Zero, (sum, detail) => sum + detail.LineTotal)
        };
        order.AddCreatedEvent();

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            orderRepo.Add(order);
            orderDetailRepo.AddRange(preparedData.Details);

            // await hybridCache.RemoveAsync($"order:{order.Id.Value}", ct);
            // await hybridCache.RemoveAsync($"order:code:{order.Code}", ct);

            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tạo đơn hàng thất bại - đã rollback");
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        // SAU COMMIT: dữ liệu đã lưu chắc chắn — các bước dưới đây không được rollback,
        // không được ném lỗi làm fail request, và không phụ thuộc CancellationToken của request nữa
        logger.LogInformation("Tạo đơn hàng {OrderId} thành công", order.Id.Value);

        await eventPublisher.PublishAfterCommitAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id.Value,
            Code = order.Code,
            CustomerId = order.CustomerId.Value,
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            CreatedAt = order.CreatedAt,
            Items = OrderEventItemMapper.ToEventItems(preparedData.Details, preparedData.Products)
        }, logger);

        return Result<OrderDto>.Success(MapToDto(order, preparedData.Details, preparedData.Products, customer.FullName));
    }

    private async Task<Result<Customer>> GetAndValidateCustomerAsync(Guid customerId, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(new CustomerId(customerId), ct);
        if (customer is not { IsActive: true })
        {
            logger.LogWarning(
                "Tạo đơn hàng thất bại — Khách hàng không tồn tại hoặc không hoạt động: {CustomerId}",
                customerId);

            return Result<Customer>.Failure("Khách hàng không tồn tại hoặc không còn hoạt động.");
        }
        return Result<Customer>.Success(customer);
    }
    private async Task<Result<CreatePreparedData>> PrepareOrderDetailsAndVerifyInventoryAsync(
        OrderId orderId, IReadOnlyList<CreateOrderItem> items, CancellationToken ct)
    {
        var products = new List<Product>();
        var details = new List<OrderDetail>();

        foreach (var item in items)
        {
            var product = await productRepo.GetByIdAsync(new ProductId(item.ProductId), ct);
            if (product is null)
            {
                return Result<CreatePreparedData>.Failure($"Sản phẩm {item.ProductId} không tồn tại.");
            }

            if (!product.IsActive)
            {
                return Result<CreatePreparedData>.Failure($"Sản phẩm '{product.Name.Value}' đã ngừng bán.");
            }

            var inventory = await inventoryRepo.GetByProductIdAsync(product.Id, ct);
            if (inventory is null)
            {
                return Result<CreatePreparedData>.Failure($"Sản phẩm '{product.Name.Value}' chưa có tồn kho.");
            }

            if (inventory.Quantity < item.Quantity)
            {
                return Result<CreatePreparedData>.Failure(
                    $"Sản phẩm '{product.Name.Value}' không đủ tồn. Còn {inventory.Quantity}, cần {item.Quantity}.");
            }

            products.Add(product);

            var existing = details.FirstOrDefault(d => d.ProductId == product.Id);
            if (existing is not null)
            {
                existing.Quantity += item.Quantity;
            }
            else
            {
                details.Add(new OrderDetail(OrderDetailId.New())
                {
                    OrderId = orderId,
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }
        }

        return Result<CreatePreparedData>.Success(new CreatePreparedData(products, details));
    }
        private OrderDto MapToDto(Order order, IReadOnlyList<OrderDetail> details, List<Product> products, string customerFullName)
    {
        var detailDtos = details.Select(detail =>
        {
            var product = products.First(p => p.Id == detail.ProductId);

            return new OrderDetailDto(
                detail.ProductId.Value,
                product.Name.Value,
                detail.Quantity,
                detail.UnitPrice.Amount,
                detail.LineTotal.Amount
            );
        }).ToList();
        
        return new OrderDto(
            order.Id.Value,
            order.Code,
            order.CustomerId.Value,
            customerFullName,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Note,
            order.CreatedAt,
            detailDtos
        );
    }
}