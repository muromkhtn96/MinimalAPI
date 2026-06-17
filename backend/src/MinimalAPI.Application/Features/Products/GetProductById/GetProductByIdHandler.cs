using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProduct;

public sealed class GetProductHandler(
    IProductRepository productRepository,
    ICacheService cacheService)                    
    : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ProductById(request.Id);

        var dto = await cacheService.GetOrCreateAsync<ProductDto?>(
            cacheKey,
            async token =>
            {
                var productId = new ProductId(request.Id);
                var product = await productRepository.GetByIdAsync(productId, token);

                if (product is null)
                {
                    return null;
                }

                return new ProductDto(
                    product.Id.Value,
                    product.Code,
                    product.Name.Value,
                    product.Price.Amount,
                    product.Price.Currency,
                    product.CategoryId.Value,
                    product.Category.Name,
                    product.Description,
                    product.IsActive,
                    product.CreatedAt);
            },
            cancellationToken: ct);
        
        if (dto is null)
        {
            return Result<ProductDto>.Failure("Không tìm thấy sản phẩm");
        }

        return Result<ProductDto>.Success(dto);

    }
}