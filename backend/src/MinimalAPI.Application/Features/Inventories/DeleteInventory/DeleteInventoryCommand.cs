using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.DeleteInventory;
/// <summary>Command xóa tồn kho.</summary>
public record DeleteInventoryCommand(
    /// <summary> ID sản phẩm liên quan đến tồn kho. </summary>
    Guid Id) : IRequest<Result<InventoryDto>>;