using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProducts;

public sealed class GetProductsHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = db.Products
            .Join(db.Categories,
                p => p.CategoryId, c => c.Id,
                (p, c) => new { Product = p, CategoryName = c.Name });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(x => x.Product.Name.Value.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.Product.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProductDto(
                x.Product.Id.Value,
                x.Product.Name.Value,
                x.Product.Price.Amount,
                x.Product.Price.Currency,
                x.Product.CategoryId.Value,
                x.CategoryName,
                x.Product.Description,
                x.Product.IsActive,
                x.Product.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(items, totalCount, request.Page, request.PageSize);
    }
}
