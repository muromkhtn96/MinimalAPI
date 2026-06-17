using MediatR;
// using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Enums;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderHandler(
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IInventoryRepository inventoryRepo,
    ICustomerRepository customerRepo,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<CancelOrderHandler> logger)
    : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý lệnh hủy đơn hàng, bao gồm kiểm tra trạng thái đơn hàng, chuẩn bị hoàn trả tồn kho nếu đã xác nhận và ghi dữ liệu trong Unit of Work
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<OrderDto>> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var orderResult = await GetAndValidateOrderForCancellationAsync(request.Id, ct);
        if (!orderResult.IsSuccess) 
            return Result<OrderDto>.Failure(orderResult.Error!);
            
        var order = orderResult.Value!;
        var customerName = await GetCustomerNameAsync(order.CustomerId, ct);
        var details = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);

        List<Inventory> restoredInventories = [];
        if (order.Status == OrderStatus.Confirmed)
        {
            var restoreResult = await PrepareInventoryRestorationAsync(details, ct);
            if (!restoreResult.IsSuccess)
                return Result<OrderDto>.Failure(restoreResult.Error!);
            restoredInventories = restoreResult.Value!;
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            if (restoredInventories.Count > 0)
            {
                inventoryRepo.UpdateRange(restoredInventories);
            }

            UpdateOrderStatusToCancelled(order);
            orderRepo.Update(order);

            await unitOfWork.CommitAsync(ct);

            await hybridCache.RemoveAsync(CacheKeys.OrderById(order.Id.Value), ct);
            await hybridCache.RemoveAsync(CacheKeys.OrderByCode(order.Code), ct);

            logger.LogInformation("Hủy đơn hàng {Id} thành công", order.Id.Value);
            
            return Result<OrderDto>.Success(MapToDto(order, details, customerName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hủy đơn {OrderId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Result<Order>> GetAndValidateOrderForCancellationAsync(Guid orderId, CancellationToken ct)
    {
        var order = await orderRepo.GetByOrderIdAsync(new OrderId(orderId), ct);
        if (order is null)
        {
            logger.LogWarning("Không tìm thấy đơn hàng {Id} để hủy đơn", orderId);
            return Result<Order>.Failure("Đơn hàng không tồn tại.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            logger.LogWarning("Đơn hàng {Id} đã bị hủy trước đó", orderId);
            return Result<Order>.Failure("Đơn hàng đã bị hủy trước đó.");
        }

        return Result<Order>.Success(order);
    }

    private async Task<string> GetCustomerNameAsync(CustomerId customerId, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(customerId, ct);
        return customer?.FullName ?? "Khách vô danh";
    }

    private async Task<Result<List<Inventory>>> PrepareInventoryRestorationAsync(
        IReadOnlyList<OrderDetail> details, CancellationToken ct)
    {
        var entities = new List<Inventory>();

        foreach (var item in details)
        {
            var exist = await inventoryRepo.GetByProductIdAsync(item.ProductId, ct);
            if (exist is null)
            {
                return Result<List<Inventory>>.Failure("Không tìm thấy tồn kho của sản phẩm.");
            }
            exist.UpdateQuantity(exist.Quantity + item.Quantity);
            entities.Add(exist);
        }

        return Result<List<Inventory>>.Success(entities);
    }

    private void UpdateOrderStatusToCancelled(Order order)
    {
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        order.AddCancelledEvent();
    }

    private OrderDto MapToDto(Order order, IReadOnlyList<OrderDetail> details, string customerName)
    {
        var detailDtos = details.Select(item => 
            new OrderDetailDto(
                item.ProductId.Value,
                string.Empty,
                item.Quantity,
                item.UnitPrice.Amount,
                item.LineTotal.Amount
            )).ToList();
            
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