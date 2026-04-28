using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Interfaces;


namespace MinimalAPI.Application.Features.Products.GetProductDeactive;

public sealed class GetProductDeactiveHandler(IProductRepository productRepository) : IRequestHandler<GetProductDeactiveQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductDeactiveQuery request, CancellationToken ct)
    {
        var products = await productRepository.GetDeactiveProductsAsync(ct);

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