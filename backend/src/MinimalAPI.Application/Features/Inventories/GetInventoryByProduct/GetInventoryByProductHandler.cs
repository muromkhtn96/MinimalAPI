using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventories.GetInventoryByProduct;

public sealed record GetInventoryByProductHandler(
    IInventoryRepository inventoryRepository,
    IProductRepository productRepository
) : IRequestHandler<GetInventoryByProductQuery, List<InventoryDto>>
{
    public async Task<List<InventoryDto>> Handle(GetInventoryByProductQuery request, CancellationToken ct)
    {
        var product = await productRepository.GetByIdAsync(request.Id, ct);

        if (product is null)
        {
            return new List<InventoryDto>();
        }

        var inventory = await inventoryRepository.GetByProductIdAsync(request.Id, ct);

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
