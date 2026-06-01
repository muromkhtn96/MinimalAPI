using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;
/// <summary> Lệnh cập nhật trạng thái không hoạt động của sản phẩm. </summary>
public sealed record UpdateProductDeactiveCommand(
    /// <summary> Mã sản phẩm cần cập nhật. </summary>
    Guid Id) : IRequest<Result<ProductDto>>;
