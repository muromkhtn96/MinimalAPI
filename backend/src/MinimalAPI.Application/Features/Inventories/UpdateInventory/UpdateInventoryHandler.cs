using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Inventories.UpdateInventory;

public sealed class UpdateInventoryHandler(
    IInventoryRepository inventoryRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateInventoryHandler> logger)
    : IRequestHandler<UpdateInventoryCommand, Result<InventoryDto>>
{
    public async Task<Result<InventoryDto>> Handle(UpdateInventoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var inventory = await inventoryRepo.GetByIdAsync(new InventoryId(request.Id), cancellationToken);
            if (inventory is null)
            {
                logger.LogInformation("Tồn kho không tồn tại. Id: {Id}", request.Id);
                return Result<InventoryDto>.Failure("Không tìm thấy bản ghi tồn kho.");
            }

            inventory.UpdateQuantity(request.Quantity);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cập nhật tồn kho thành công. Id: {Id}", request.Id);

            var inventoryDto = new InventoryDto(
                inventory.Id.Value,
                inventory.ProductId.Value,
                inventory.Quantity);

            return Result<InventoryDto>.Success(inventoryDto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi cập nhật tồn kho. Id: {Id}", request.Id);
            return Result<InventoryDto>.Failure("Đã xảy ra lỗi khi cập nhật tồn kho.");
        }
    }
}
