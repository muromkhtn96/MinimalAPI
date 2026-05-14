using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.CreateInventory;

public record CreateInventoryCommand(
    Guid Id,
    long Quantity) : IRequest<Result<InventoryDto>>;
