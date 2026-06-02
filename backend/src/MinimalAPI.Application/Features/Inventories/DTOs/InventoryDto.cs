namespace MinimalAPI.Application.Features.Inventories.DTO;

public record InventoryDto(
    /// <summary>Mã tồn kho.</summary>
    Guid Id,
    /// <summary>Mã sản phẩm.</summary>
    Guid ProductId,
    /// <summary>Số lượng tồn kho.</summary>
    long Quantity
);