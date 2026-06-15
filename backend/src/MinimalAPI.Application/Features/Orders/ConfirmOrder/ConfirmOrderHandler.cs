using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
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

namespace MinimalAPI.Application.Features.Orders.ConfirmOrder;

public sealed class ConfirmOrderHandler(
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IInventoryRepository inventoryRepo,
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    // HybridCache hybridCache,
    ILogger<ConfirmOrderHandler> logger)
    : IRequestHandler<ConfirmOrderCommand, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý lệnh xác nhận đơn hàng, bao gồm kiểm tra trạng thái đơn hàng, kiểm tra tồn kho và cập nhật trạng thái đơn hàng
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<OrderDto>> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var order = await orderRepo.GetByOrderIdAsync(new OrderId(request.Id), ct);
        if (order is null)
        {
            logger.LogWarning("Không tìm thấy đơn hàng {Id} để xác nhận", request.Id);
            return Result<OrderDto>.Failure("Đơn hàng không tồn tại.");
        }

        if (order.Status == OrderStatus.Confirmed)
        {
            logger.LogWarning("Đơn hàng {Id} đã xác nhận trước đó", request.Id);
            return Result<OrderDto>.Failure("Đơn hàng đã xác nhận trước đó.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            logger.LogWarning("Đơn hàng {Id} đã bị hủy nên không thể xác nhận", request.Id);
            return Result<OrderDto>.Failure("Không thể xác nhận đơn hàng đã bị hủy.");
        }

        var customer = await customerRepo.GetByIdAsync(order.CustomerId, ct);

        var details = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);
        if (details.Count == 0)
        {
            return Result<OrderDto>.Failure("Không thể xác nhận đơn hàng rỗng.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        
        try
        {
            var entities = new List<Inventory>();
            var products = new List<Product>();

            foreach (var item in details)
            {
                var exist = await inventoryRepo.GetByProductIdAsync(item.ProductId, ct);
                if (exist is null)
                {
                    return Result<OrderDto>.Failure($"Sản phẩm {item.ProductId.Value} chưa có tồn kho.");
                }

                if (exist.Quantity < item.Quantity)
                {
                    return Result<OrderDto>.Failure(
                        $"Sản phẩm không đủ tồn kho để xác nhận đơn. Còn {exist.Quantity}, cần {item.Quantity}.");
                }

                exist.UpdateQuantity(exist.Quantity - item.Quantity);
                entities.Add(exist);

                var product = await productRepo.GetByIdAsync(item.ProductId, ct);
                if (product is not null)
                {
                    products.Add(product);
                }
            }

            inventoryRepo.UpdateRange(entities);

            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.UtcNow;
            order.AddConfirmedEvent();

            orderRepo.Update(order);

            await unitOfWork.CommitAsync(ct);

            // await hybridCache.RemoveAsync($"order:{order.Id.Value}", ct);
            // await hybridCache.RemoveAsync($"order:code:{order.Code}", ct);

            logger.LogInformation("Xác nhận đơn hàng {Id} thành công, đã trừ tồn kho", order.Id.Value);
            
            var customerName = customer?.FullName ?? "Khách vô danh";

            var detailDtos = details.Select(item =>
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                return new OrderDetailDto(
                    item.ProductId.Value,
                    product?.Name.Value ?? "Sản phẩm không xác định",
                    item.Quantity,
                    item.UnitPrice.Amount,
                    item.LineTotal.Amount
                );
            }).ToList();
            
            return Result<OrderDto>.Success(
                new OrderDto(
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
                ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xác nhận đơn {OrderId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}