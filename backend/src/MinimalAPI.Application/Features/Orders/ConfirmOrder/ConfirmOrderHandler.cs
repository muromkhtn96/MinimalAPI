using MediatR;
// using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Enums;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Orders.ConfirmOrder;

public sealed class ConfirmOrderHandler(
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IInventoryRepository inventoryRepo,
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<ConfirmOrderHandler> logger)
    : IRequestHandler<ConfirmOrderCommand, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý lệnh xác nhận đơn hàng, bao gồm kiểm tra trạng thái đơn hàng, chuẩn bị trừ tồn kho và ghi dữ liệu trong Unit of Work
    /// </summary>
    /// <param name="Inventories"></param>
    /// <param name="Products"></param>
    private record ConfirmPreparedData(List<Inventory> Inventories, List<Product> Products);
    public async Task<Result<OrderDto>> Handle(ConfirmOrderCommand request, CancellationToken ct)
    {
        var orderResult = await GetAndValidateOrderForConfirmationAsync(request.Id, ct);
        if (!orderResult.IsSuccess)
            return Result<OrderDto>.Failure(orderResult.Error!);
        var order = orderResult.Value!;
        var customerName = await GetCustomerNameAsync(order.CustomerId, ct);

        var details = await orderDetailRepo.GetByOrderIdAsync(order.Id, ct);
        if (details.Count == 0)
        {
            return Result<OrderDto>.Failure("Không thể xác nhận đơn hàng rỗng.");
        }

        var prepareResult = await PrepareInventoryDeductionAsync(details, ct);
        if (!prepareResult.IsSuccess)
            return Result<OrderDto>.Failure(prepareResult.Error!);
        var preparedData = prepareResult.Value!;

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        
        try
        {
            inventoryRepo.UpdateRange(preparedData.Inventories);

            UpdateOrderStatusToConfirmed(order);
            orderRepo.Update(order);

            await unitOfWork.CommitAsync(ct);
            
            await hybridCache.RemoveAsync(CacheKeys.OrderById(order.Id.Value), ct);
            await hybridCache.RemoveAsync(CacheKeys.OrderByCode(order.Code), ct);

            logger.LogInformation("Xác nhận đơn hàng {Id} thành công, đã trừ tồn kho", order.Id.Value);
            
            return Result<OrderDto>.Success(MapToDto(order, details, preparedData.Products, customerName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xác nhận đơn {OrderId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
    private async Task<Result<Order>> GetAndValidateOrderForConfirmationAsync(Guid orderId, CancellationToken ct)
    {
        var order = await orderRepo.GetByOrderIdAsync(new OrderId(orderId), ct);
        if (order is null)
        {
            logger.LogWarning("Không tìm thấy đơn hàng {Id} để xác nhận", orderId);
            return Result<Order>.Failure("Đơn hàng không tồn tại.");
        }
        if (order.Status == OrderStatus.Confirmed)
        {
            logger.LogWarning("Đơn hàng {Id} đã xác nhận trước đó", orderId);
            return Result<Order>.Failure("Đơn hàng đã xác nhận trước đó.");
        }
        if (order.Status == OrderStatus.Cancelled)
        {
            logger.LogWarning("Đơn hàng {Id} đã bị hủy nên không thể xác nhận", orderId);
            return Result<Order>.Failure("Không thể xác nhận đơn hàng đã bị hủy.");
        }
        return Result<Order>.Success(order);
    }
    private async Task<string> GetCustomerNameAsync(CustomerId customerId, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(customerId, ct);
        return customer?.FullName ?? "Khách vô danh";
    }
    private async Task<Result<ConfirmPreparedData>> PrepareInventoryDeductionAsync(
        IReadOnlyList<OrderDetail> details, CancellationToken ct)
    {
        var entities = new List<Inventory>();
        var products = new List<Product>();
        foreach (var item in details)
        {
            var exist = await inventoryRepo.GetByProductIdAsync(item.ProductId, ct);
            if (exist is null)
            {
                return Result<ConfirmPreparedData>.Failure($"Sản phẩm {item.ProductId.Value} chưa có tồn kho.");
            }
            if (exist.Quantity < item.Quantity)
            {
                return Result<ConfirmPreparedData>.Failure(
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
        return Result<ConfirmPreparedData>.Success(new ConfirmPreparedData(entities, products));
    }
    private void UpdateOrderStatusToConfirmed(Order order)
    {
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;
        order.AddConfirmedEvent();
    }
    private OrderDto MapToDto(Order order, IReadOnlyList<OrderDetail> details, List<Product> products, string customerName)
    {
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