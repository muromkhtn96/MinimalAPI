using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProduct;
/// <summary>
/// Lấy thông tin sản phẩm theo ID
/// </summary>
/// <param name="Id"></param>
public record GetProductByIdQuery(
    /// <summary>Mã sản phẩm.</summary>
    Guid Id) : IRequest<Result<ProductDto>>;
