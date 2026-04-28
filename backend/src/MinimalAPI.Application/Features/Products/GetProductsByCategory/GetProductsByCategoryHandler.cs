using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProductsByCategory;

public sealed class GetProductsByCategoryHandler(IProductRepository productRepo)
    : IRequestHandler<GetProductsByCategoryQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductsByCategoryQuery request, CancellationToken ct)
    {
        var products = await productRepo.GetByCategoryAsync(new CategoryId(request.CategoryId), ct);

        var dtos = products
            .Select(p => new ProductDto(
                p.Id.Value,
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