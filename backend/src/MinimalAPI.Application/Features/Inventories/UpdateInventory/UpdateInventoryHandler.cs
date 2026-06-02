using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Inventories.UpdateInventory;

public sealed class UpdateInventoryHandler(
    IInventoryRepository inventoryRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<UpdateInventoryHandler> logger)
    : IRequestHandler<UpdateInventoryCommand, Result<InventoryDto>>
{
    /// <summary>
    /// Xử lý lệnh cập nhật thông tin tồn kho
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<InventoryDto>> Handle(UpdateInventoryCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepo.GetByIdAsync(new InventoryId(request.Id), ct);
        if (inventory is null)
        {
            logger.LogInformation("Tồn kho không tồn tại. Id: {Id}", request.Id);
            return Result<InventoryDto>.Failure("Không tìm thấy bản ghi tồn kho.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            inventory.UpdateQuantity(request.Quantity);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Cập nhật tồn kho thành công. Id: {Id}", request.Id);

            return Result<InventoryDto>.Success(new InventoryDto(
                inventory.Id.Value,
                inventory.ProductId.Value,
                inventory.Quantity));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cập nhật tồn kho thất bại - đã rollback. Id: {Id}", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
