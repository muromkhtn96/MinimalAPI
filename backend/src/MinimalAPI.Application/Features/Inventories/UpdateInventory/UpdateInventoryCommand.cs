using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Inventories.DTO;

namespace MinimalAPI.Application.Features.Inventories.UpdateInventory;   
/// <summary> Cập nhật số lượng tồn kho dựa trên ID tồn kho. </summary>
public record UpdateInventoryCommand(
    /// <summary> ID tồn kho cần cập nhật. </summary>
    Guid Id, 
    /// <summary> Số lượng tồn kho mới. </summary>
    int Quantity) : IRequest<Result<InventoryDto>>;