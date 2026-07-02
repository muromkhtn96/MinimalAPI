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
using MinimalAPI.Domain.ValueObjects;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.UpdateOrder;
public sealed class UpdateOrderHandler(
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo,
    ICustomerRepository customerRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ICacheService cacheService,
    IIntegrationEventPublisher eventPublisher,
    ILogger<UpdateOrderHandler> logger)
    : IRequestHandler<UpdateOrderCommand, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý lệnh cập nhật đơn hàng, bao gồm kiểm tra trạng thái đơn hàng, chuẩn bị chi tiết mới và validate tồn kho ngoài Unit of Work, sau đó ghi dữ liệu trong Unit of Work
    /// </summary>
    /// <param name="Products"></param>
    /// <param name="NewDetails"></param>
    private record UpdatePreparedData(List<Product> Products, List<OrderDetail> NewDetails);

    public async Task<Result<OrderDto>> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var orderResult = await GetAndValidateOrderForUpdateAsync(request.OrderId, ct);
        if (!orderResult.IsSuccess)
            return Result<OrderDto>.Failure(orderResult.Error!);
        var order = orderResult.Value!;

        var prepareResult = await PrepareNewDetailsAndValidateInventoryAsync(order.Id, request.Details, ct);
        if (!prepareResult.IsSuccess)
            return Result<OrderDto>.Failure(prepareResult.Error!);
        var preparedData = prepareResult.Value!;

        var customerName = await GetCustomerNameAsync(order.CustomerId, ct);
        var existingDetails = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            orderDetailRepo.RemoveRange(existingDetails);
            orderDetailRepo.AddRange(preparedData.NewDetails);

            order.Note = request.Note?.Trim();
            order.Details = preparedData.NewDetails;
            order.TotalAmount = preparedData.NewDetails.Aggregate(Money.Zero, (sum, d) => sum + d.LineTotal);
            order.UpdatedAt = DateTime.UtcNow;

            orderRepo.Update(order);
            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cập nhật đơn hàng {OrderId} thất bại - đã rollback", request.OrderId);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        // SAU COMMIT: dữ liệu đã lưu chắc chắn — các bước dưới đây không được rollback,
        // không được ném lỗi làm fail request, và không phụ thuộc CancellationToken của request nữa.
        // (Trước đây cache lỗi sau commit → rollback trên transaction ĐÃ commit + trả 500 sai)
        logger.LogInformation("Cập nhật đơn hàng {OrderId} thành công", order.Id.Value);

        try
        {
            await cacheService.RemoveAsync(CacheKeys.OrderById(order.Id.Value), CancellationToken.None);
            await cacheService.RemoveAsync(CacheKeys.OrderByCode(order.Code), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Xóa cache đơn hàng {OrderId} thất bại — dữ liệu cache có thể cũ tối đa hết TTL",
                order.Id.Value);
        }

        await eventPublisher.PublishAfterCommitAsync(new OrderUpdatedIntegrationEvent
        {
            OrderId = order.Id.Value,
            Code = order.Code,
            CustomerId = order.CustomerId.Value,
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            UpdatedAt = order.UpdatedAt ?? DateTime.UtcNow,
            Items = OrderEventItemMapper.ToEventItems(preparedData.NewDetails, preparedData.Products)
        }, logger);

        return Result<OrderDto>.Success(MapToDto(order, preparedData.NewDetails, preparedData.Products, customerName));
    }

    private async Task<Result<Order>> GetAndValidateOrderForUpdateAsync(Guid orderId, CancellationToken ct)
    {
        var order = await orderRepo.GetByOrderIdAsync(new OrderId(orderId), ct);
        if (order is null)
        {
            return Result<Order>.Failure("Đơn hàng không tồn tại.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            return Result<Order>.Failure("Chỉ được chỉnh sửa khi đơn hàng đang ở trạng thái chờ (Pending).");
        }

        return Result<Order>.Success(order);
    }

    private async Task<Result<UpdatePreparedData>> PrepareNewDetailsAndValidateInventoryAsync(
        OrderId orderId, IReadOnlyList<OrderDetailDto> requestDetails, CancellationToken ct)
    {
        var newItems = requestDetails
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        var products = new List<Product>();
        var newDetails = new List<OrderDetail>();

        foreach (var newItem in newItems)
        {
            var productId = new ProductId(newItem.ProductId);
            var product = await productRepo.GetByIdAsync(productId, ct);
            if (product is null)
                return Result<UpdatePreparedData>.Failure($"Sản phẩm {newItem.ProductId} không tồn tại.");
            if (!product.IsActive)
                return Result<UpdatePreparedData>.Failure($"Sản phẩm '{product.Name.Value}' đã ngừng bán.");

            products.Add(product);

            var inventory = await inventoryRepo.GetByProductIdAsync(productId, ct);
            if (inventory is null)
                return Result<UpdatePreparedData>.Failure($"Sản phẩm '{product.Name.Value}' chưa có tồn kho.");

            if (inventory.Quantity < newItem.Quantity)
            {
                return Result<UpdatePreparedData>.Failure(
                    $"Sản phẩm '{product.Name.Value}' không đủ tồn kho (còn {inventory.Quantity}, cần {newItem.Quantity}).");
            }

            newDetails.Add(new OrderDetail(OrderDetailId.New())
            {
                OrderId = orderId,
                ProductId = productId,
                Quantity = newItem.Quantity,
                UnitPrice = product.Price
            });
        }

        return Result<UpdatePreparedData>.Success(new UpdatePreparedData(products, newDetails));
    }

    private async Task<string> GetCustomerNameAsync(CustomerId customerId, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(customerId, ct);
        return customer?.FullName ?? "Khách vô danh";
    }

    private OrderDto MapToDto(Order order, IReadOnlyList<OrderDetail> details, List<Product> products, string customerName)
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
            customerName,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.Note,
            order.CreatedAt,
            detailDtos
        );
    }
}