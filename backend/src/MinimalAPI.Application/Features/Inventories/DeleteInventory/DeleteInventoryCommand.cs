using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.DeleteInventory;

public record DeleteInventoryCommand(
    /// <summary>Mã tồn kho cần xóa.</summary>
    Guid Id) : IRequest<Result<InventoryDto>>;