using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProductByCode; 
public sealed class GetProductByCodeHandler(
    IProductRepository productRepository,
    ICacheService cacheService)
    : IRequestHandler<GetProductByCodeQuery, Result<ProductDto>>
{
    /// <summary>
    /// Xử lý truy vấn lấy sản phẩm theo mã.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<ProductDto>> Handle(GetProductByCodeQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.ProductByCode(request.Code);

        var dto = await cacheService.GetOrCreateAsync<ProductDto?>(
            cacheKey,
            async token =>
            {
                var product = await productRepository.GetByCodeAsync(request.Code, token);
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