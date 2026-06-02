using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProductDeactive;
/// <summary> Truy vấn lấy danh sách sản phẩm không hoạt động. </summary>
public sealed record GetProductDeactiveQuery() : IRequest<Result<List<ProductDto>>>;