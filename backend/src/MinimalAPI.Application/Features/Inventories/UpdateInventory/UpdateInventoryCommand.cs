using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.UpdateInventory;

public record UpdateInventoryCommand(Guid Id,int Quantity): IRequest<Result<InventoryDto>>;