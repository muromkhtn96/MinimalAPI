using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductByCode;
/// <summary> Truy vấn lấy sản phẩm theo mã. </summary>
public sealed record GetProductByCodeQuery(
    /// <summary> Mã sản phẩm cần tìm. </summary>
    string Code) : IRequest<Result<ProductDto>>;