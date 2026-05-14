using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Inventories.GetInventories;

public sealed record GetInvenoriesHandler(
    IInventoryRepository inventoryRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<GetInventoriesQuery, List<InventoryDto>>
{
    public async Task<List<InventoryDto>> Handle(GetInventoriesQuery request, CancellationToken ct)
    {
        var inventories = await inventoryRepository.GetAllAsync(ct);

        var inventoryDtos = new List<InventoryDto>();

        foreach (var inventory in inventories)
        {
            var product = await productRepository.GetByIdAsync(inventory.ProductId, ct);
            if (product is not null)
            {
                inventoryDtos.Add(new InventoryDto(
                    inventory.Id.Value,
                    inventory.ProductId.Value,
                    inventory.Quantity
                ));
            }
        }

        return inventoryDtos;
    }
}