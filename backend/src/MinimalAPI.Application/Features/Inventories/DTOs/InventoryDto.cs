namespace MinimalAPI.Application.Features.Inventories.DTO;

public record InventoryDto(
    Guid Id,
    Guid ProductId,
    long Quantity
);