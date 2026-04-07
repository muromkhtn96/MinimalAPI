using MediatR;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProduct;

/// <summary>Query lấy chi tiết sản phẩm theo Id.</summary>
public record GetProductQuery(
    /// <summary>Mã sản phẩm cần lấy.</summary>
    Guid Id) : IRequest<ProductDto?>;
