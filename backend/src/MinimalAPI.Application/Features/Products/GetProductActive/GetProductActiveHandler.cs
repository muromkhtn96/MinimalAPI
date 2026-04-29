using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProductActive;

public sealed class GetProductActiveHandler(IProductRepository productRepo) : IRequestHandler<GetProductActiveQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductActiveQuery request, CancellationToken ct)
    {
        Task<List<Product>> getActiveProductsTask = productRepo.GetActiveProductsAsync(ct); 
        List<Product> products = await getActiveProductsTask;

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
