using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Inventories.GetInventory;

public record GetInventoryQuery(InventoryId Id) : IRequest<List<InventoryDto>>;
