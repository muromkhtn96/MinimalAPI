using MediatR;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Enums;
using MinimalAPI.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalAPI.Application.Features.Orders.CancelOrder;

public sealed class CancelOrderHandler(
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IInventoryRepository inventoryRepo,
    ICustomerRepository customerRepo,
    IUnitOfWorkManager unitOfWorkManager,
    // HybridCache hybridCache,
    ILogger<CancelOrderHandler> logger)
    : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý lệnh hủy đơn hàng, bao gồm kiểm tra trạng thái đơn hàng
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

        var previousStatus = order.Status;
        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct
        );
        try
        {
            if (previousStatus == OrderStatus.Confirmed)
            {
                var restoreResult = await RestoreInventoryStockAsync(order.Id, ct);
                if (!restoreResult.IsSuccess) 
                    return Result<OrderDto>.Failure(restoreResult.Error!);
            }

            UpdateOrderStatusToCancelled(order);
            orderRepo.Update(order);

            await unitOfWork.CommitAsync(ct);
            logger.LogInformation("Hủy đơn hàng {Id} thành công", order.Id.Value);
            
            var details = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);
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

    private async Task<Result<bool>> RestoreInventoryStockAsync(OrderId orderId, CancellationToken ct)
    {
        var details = await orderDetailRepo.GetByOrderIdAsync(orderId, ct);
        var entities = new List<Inventory>();

        foreach (var item in details)
        {
            var exist = await inventoryRepo.GetByProductIdAsync(item.ProductId, ct);
            if (exist is null)
            {
                return Result<bool>.Failure("Không tìm thấy tồn kho của sản phẩm.");
            }
            exist.UpdateQuantity(exist.Quantity + item.Quantity);
            entities.Add(exist);
        }

        inventoryRepo.UpdateRange(entities);
        return Result<bool>.Success(true);
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