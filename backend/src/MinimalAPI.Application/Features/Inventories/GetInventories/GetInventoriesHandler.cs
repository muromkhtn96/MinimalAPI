using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventories.GetInventories;

public sealed record GetInventoriesHandler(
    IInventoryRepository inventoryRepository,
    IProductRepository productRepository
) : IRequestHandler<GetInventoriesQuery, List<InventoryDto>>
{
    /// <summary>
    /// Xử lý lệnh lấy danh sách tồn kho
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<List<InventoryDto>> Handle(GetInventoriesQuery request, CancellationToken ct)
    {
        var inventories = await inventoryRepository.GetAllAsync(ct);

        return inventories.Select(inventory => new InventoryDto(
            inventory.Id.Value,
            inventory.ProductId.Value,
            inventory.Quantity
        )).ToList();
    }
}