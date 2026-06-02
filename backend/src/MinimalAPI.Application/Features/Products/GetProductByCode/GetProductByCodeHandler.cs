using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProductByCode; 
public sealed class GetProductByCodeHandler(
    IProductRepository productRepository,
    ILogger<GetProductByCodeHandler> logger)
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
        var product = await productRepository.GetByCodeAsync(request.Code, ct);

        if (product is null)
        {
            logger.LogInformation("Không tìm thấy sản phẩm với mã Code: {Code}", request.Code);
            return Result<ProductDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var dto = new ProductDto(
            product.Id .Value,
            product.Code,
            product.Name.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.CategoryId.Value,
            product.Category.Name,
            product.Description,
            product.IsActive,
            product.CreatedAt
        );

        return Result<ProductDto>.Success(dto);
    }
}