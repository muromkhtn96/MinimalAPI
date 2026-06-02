using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductActive;
/// <summary>
/// Truy vấn lấy danh sách sản phẩm hoạt động.
/// </summary>
public sealed record GetProductActiveQuery() : IRequest<Result<List<ProductDto>>>;