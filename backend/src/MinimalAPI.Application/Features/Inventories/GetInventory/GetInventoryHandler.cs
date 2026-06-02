using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventories.GetInventory;

public sealed record GetInventoryHandler(
    IInventoryRepository inventoryRepository,
    IProductRepository productRepository
) : IRequestHandler<GetInventoryQuery, List<InventoryDto>>
{
    /// <summary>
    /// Xử lý lệnh lấy thông tin tồn kho
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<List<InventoryDto>> Handle(GetInventoryQuery request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByIdAsync(request.Id, ct);

        if (inventory is null)
        {
            return new List<InventoryDto>();
        }

        return new List<InventoryDto>
        {
            new InventoryDto(
                inventory.Id.Value,
                inventory.ProductId.Value,
                inventory.Quantity)
        };
    }
}
