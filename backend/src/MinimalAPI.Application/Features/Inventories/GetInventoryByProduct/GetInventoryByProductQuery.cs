using MediatR;
using MinimalAPI.Application.Features.Inventories.DTO;
using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Features.Inventories.GetInventoryByProduct;
/// <summary> Lấy thông tin tồn kho dựa trên ID sản phẩm. </summary>
public record GetInventoryByProductQuery(ProductId Id) : IRequest<List<InventoryDto>>;
