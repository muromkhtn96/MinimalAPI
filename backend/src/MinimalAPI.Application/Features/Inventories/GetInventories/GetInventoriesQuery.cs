using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.GetInventories;

public sealed record GetInventoriesQuery : IRequest<List<InventoryDto>>;
