using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProducts;

public sealed class GetProductsHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var totalCount = await productRepository.CountAsync(request.Search, ct);
        var products = await productRepository.GetPagedAsync(request.Page, request.PageSize, request.Search, ct);

        var items = products
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

        return new PagedResult<ProductDto>(items, totalCount, request.Page, request.PageSize);
    }
}
