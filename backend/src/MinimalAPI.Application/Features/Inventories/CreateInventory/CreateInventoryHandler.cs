using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventories.CreateInventory;

public sealed class CreateInventoryHandler(
    IInventoryRepository inventoryRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateInventoryHandler> logger)
    : IRequestHandler<CreateInventoryCommand, Result<InventoryDto>>
{
    public async Task<Result<InventoryDto>> Handle(
        CreateInventoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var productId = new ProductId(request.Id);

            var product = await productRepository.GetByIdAsync(
                productId,
                cancellationToken);

            if (product is null)
            {
                logger.LogWarning(
                    "Sản phẩm không tồn tại. Id: {ProductId}",
                    request.Id);

                return Result<InventoryDto>.Failure(
                    "Sản phẩm không tồn tại.");
            }

            var exists = await inventoryRepository.ExistsByProductIdAsync(
                productId,
                cancellationToken);

            if (exists)
            {
                logger.LogWarning(
                    "Tồn kho của sản phẩm này đã tồn tại. ProductId: {ProductId}",
                    request.Id);

                return Result<InventoryDto>.Failure(
                    "Tồn kho của sản phẩm này đã tồn tại.");
            }

            var inventory = Inventory.Create(
                productId,
                request.Quantity);

            inventoryRepository.Add(inventory);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Tạo mới tồn kho thành công. ProductId: {ProductId}",
                request.Id);

            return Result<InventoryDto>.Success(
                new InventoryDto(
                    inventory.Id.Value,
                    inventory.ProductId.Value,
                    inventory.Quantity));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Lỗi khi tạo tồn kho cho ProductId: {ProductId}",
                request.Id);

            return Result<InventoryDto>.Failure(
                "Đã xảy ra lỗi hệ thống.");
        }
    }
}