using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Orders.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Enums;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalAPI.Application.Features.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    ICustomerRepository customerRepo,
    IProductRepository productRepo,
    IInventoryRepository inventoryRepo,
    IOrderRepository orderRepo,
    IOrderDetailRepository orderDetailRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ICodeGenerator codeGenerator,
    ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    /// <summary>
    /// Xử lý lệnh tạo đơn hàng mới, bao gồm kiểm tra thông tin khách hàng
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(new CustomerId(request.CustomerId), ct);
        if (customer is not { IsActive: true })
        {
            logger.LogWarning(
                "Tạo đơn hàng thất bại — Khách hàng không tồn tại hoặc không hoạt động: {CustomerId}",
                request.CustomerId);

            return Result<OrderDto>.Failure("Khách hàng không tồn tại hoặc không còn hoạt động.");
        }

        var code = await codeGenerator.NextAsync("DH", 5, ct);
        var orderId = OrderId.New();

        var products = new List<Product>();
        var details = new List<OrderDetail>();

        foreach (var item in request.Items)
        {
            var product = await productRepo.GetByIdAsync(new ProductId(item.ProductId), ct);
            if (product is null)
            {
                return Result<OrderDto>.Failure($"Sản phẩm {item.ProductId} không tồn tại.");
            }

            if (!product.IsActive)
            {
                return Result<OrderDto>.Failure($"Sản phẩm '{product.Name.Value}' đã ngừng bán.");
            }

            var inventory = await inventoryRepo.GetByProductIdAsync(product.Id, ct);
            if (inventory is null)
            {
                return Result<OrderDto>.Failure($"Sản phẩm '{product.Name.Value}' chưa có tồn kho.");
            }

            if (inventory.Quantity < item.Quantity)
            {
                return Result<OrderDto>.Failure(
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

        if (details.Count == 0)
        {
            return Result<OrderDto>.Failure("Đơn hàng phải có ít nhất một sản phẩm.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);

        try
        {
            var order = new Order(orderId)
            {
                Code = code,
                CustomerId = customer.Id,
                Status = OrderStatus.Pending,
                Note = request.Note?.Trim(),
                CreatedAt = DateTime.UtcNow,
                Details = details,
                TotalAmount = details.Aggregate(Money.Zero, (sum, detail) => sum + detail.LineTotal)
            };

            order.AddCreatedEvent();

            orderRepo.Add(order);
            orderDetailRepo.AddRange(details);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Tạo đơn hàng {OrderId} thành công", order.Id.Value);

            var detailDtos = order.Details.Select(detail =>
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

            return Result<OrderDto>.Success(
                new OrderDto(
                    order.Id.Value,
                    order.Code,
                    order.CustomerId.Value,
                    customer.FullName,
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
            logger.LogError(ex, "Tạo đơn hàng thất bại - đã rollback");
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}