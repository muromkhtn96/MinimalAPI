using MediatR;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProduct;

public sealed class GetProductHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken ct)    
    {
        var productId = new ProductId(request.Id);
        var product = await productRepository.GetByIdAsync(productId, ct);

        return product is not null
            ? new ProductDto(
                product.Id.Value,
                product.Name.Value,
                product.Price.Amount,
                product.Price.Currency,
                product.CategoryId.Value,
                product.Category.Name,
                product.Description,
                product.IsActive,
                product.CreatedAt)
            : null;
    }
}
