using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductByCode;
/// <summary>
/// Lấy thông tin sản phẩm theo mã code
/// </summary>
/// <param name="Code"></param>
public sealed record GetProductByCodeQuery(
    /// <summary> Mã code sản phẩm. </summary>
    string Code) : IRequest<Result<ProductDto>>;