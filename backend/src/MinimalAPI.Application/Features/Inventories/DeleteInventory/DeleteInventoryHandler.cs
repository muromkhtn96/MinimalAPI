using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventories.DeleteInventory;

public sealed class DeleteInventoryHandler(
    IInventoryRepository inventoryRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<DeleteInventoryHandler> logger)
    : IRequestHandler<DeleteInventoryCommand, Result<InventoryDto>>
{
    /// <summary>
    /// Xử lý lệnh xóa tồn kho
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<InventoryDto>> Handle(DeleteInventoryCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepo.GetByIdAsync(new InventoryId(request.Id), ct);
        if (inventory is null)
        {
            logger.LogInformation("Tồn kho không tồn tại. Id: {Id}", request.Id);
            return Result<InventoryDto>.Failure("Tồn kho không tồn tại.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            inventoryRepo.Remove(inventory);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Xóa tồn kho thành công. Id: {Id}", request.Id);

            return Result<InventoryDto>.Success(new InventoryDto(
                inventory.Id.Value,
                inventory.ProductId.Value,
                inventory.Quantity));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xóa tồn kho thất bại - đã rollback. Id: {Id}", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
