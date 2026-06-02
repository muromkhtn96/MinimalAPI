using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductsByCategory;
/// <summary> Truy vấn lấy danh sách sản phẩm theo mã danh mục. </summary>
public sealed record GetProductsByCategoryIdQuery(
    /// <summary> Mã danh mục cần tìm. </summary>
    Guid CategoryId) : IRequest<Result<List<ProductDto>>>;