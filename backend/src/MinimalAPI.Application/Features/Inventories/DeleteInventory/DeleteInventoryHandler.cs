using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DeleteInventory;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventories.DeleteInventory;

public sealed class DeleteInventoryHandler(
    IInventoryRepository inventoryRepo,
    IUnitOfWork unitOfWork,
    ILogger<DeleteInventoryHandler> logger)
    : IRequestHandler<DeleteInventoryCommand, Result<InventoryDto>>
{
    public async Task<Result<InventoryDto>> Handle(DeleteInventoryCommand request, CancellationToken ct)
    {
        try
        {
            var inventory = await inventoryRepo.GetByIdAsync(new InventoryId(request.Id), ct);

            if (inventory is null)
            {
                logger.LogInformation("Tồn kho không tồn tại. Id: {Id}", request.Id);
                return Result<InventoryDto>.Failure("Tồn kho không tồn tại.");
            }

            inventoryRepo.Remove(inventory);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation("Xóa tồn kho thành công. Id: {Id}", request.Id);

            return Result<InventoryDto>.Success(new InventoryDto(
                inventory.Id.Value,
                inventory.ProductId.Value,
                inventory.Quantity));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi xóa tồn kho. Id: {Id}", request.Id);
            return Result<InventoryDto>.Failure("Đã xảy ra lỗi khi xóa tồn kho.");
        }
    }
}
