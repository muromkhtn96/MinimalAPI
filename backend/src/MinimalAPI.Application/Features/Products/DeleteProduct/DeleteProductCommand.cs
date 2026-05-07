using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
namespace MinimalAPI.Application.Features.Products.DeleteProduct;

/// <summary>Command xóa sản phẩm.</summary>
public record DeleteProductCommand(
    /// <summary>Mã sản phẩm cần xóa.</summary>
    Guid Id) : IRequest<Result<ProductDto>>;
