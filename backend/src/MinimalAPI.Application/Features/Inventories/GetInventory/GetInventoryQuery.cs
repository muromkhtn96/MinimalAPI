using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Entities;
    
namespace MinimalAPI.Application.Features.Inventories.GetInventory;
/// <summary> Lấy thông tin tồn kho dựa trên ID tồn kho. </summary>
public record GetInventoryQuery(InventoryId Id) : IRequest<List<InventoryDto>>;
