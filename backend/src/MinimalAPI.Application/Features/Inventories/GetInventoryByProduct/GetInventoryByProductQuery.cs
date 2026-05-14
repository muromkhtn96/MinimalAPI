using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Inventories.GetInventoryByProduct;

public record GetInventoryByProductQuery(ProductId Id) : IRequest<List<InventoryDto>>;
