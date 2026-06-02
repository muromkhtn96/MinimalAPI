using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProductsByCategory;

public sealed class GetProductsByCategoryIdHandler(IProductRepository productRepo)
    : IRequestHandler<GetProductsByCategoryIdQuery, Result<List<ProductDto>>>
{
    /// <summary>
    /// Xử lý lệnh lấy danh sách sản phẩm theo danh mục.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<List<ProductDto>>> Handle(GetProductsByCategoryIdQuery request, CancellationToken ct)
    {
        var products = await productRepo.GetByCategoryIdAsync(new CategoryId(request.CategoryId), ct);

        var dtos = products
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Code,
                p.Name.Value,
                p.Price.Amount,
                p.Price.Currency,
                p.CategoryId.Value,
                p.Category.Name,
                p.Description,
                p.IsActive,
                p.CreatedAt))
            .ToList();

        return Result<List<ProductDto>>.Success(dtos);
    }
}