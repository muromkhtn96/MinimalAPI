using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.CreateInventory;
/// <summary>Command tạo tồn kho mới.</summary>
public record CreateInventoryCommand(
    /// <summary> ID sản phẩm liên quan đến tồn kho. </summary>
    Guid Id,
    /// <summary> Số lượng tồn kho. </summary>
    long Quantity) : IRequest<Result<InventoryDto>>;
