using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.GetProductActive;

public sealed class GetProductActiveHandler(
    IProductRepository productRepository)
    : IRequestHandler<GetProductActiveQuery, Result<List<ProductDto>>>
{
    /// <summary>
    /// Xử lý truy vấn lấy danh sách sản phẩm hoạt động.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<List<ProductDto>>> Handle(GetProductActiveQuery request, CancellationToken ct)
    {
        List<Product> products = await productRepository.GetActiveProductsAsync(ct);

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