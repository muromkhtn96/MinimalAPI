using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.GetInventories;
/// <summary> Lấy danh sách tồn kho.</summary>
public sealed record GetInventoriesQuery : IRequest<List<InventoryDto>>;
