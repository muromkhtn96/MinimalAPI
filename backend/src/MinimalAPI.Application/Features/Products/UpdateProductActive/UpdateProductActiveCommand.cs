using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;
/// <summary> Lệnh cập nhật trạng thái hoạt động của sản phẩm. </summary>
public sealed record UpdateProductActiveCommand(
    /// <summary> Mã sản phẩm cần cập nhật. </summary>
    Guid Id) : IRequest<Result<ProductDto>>;
