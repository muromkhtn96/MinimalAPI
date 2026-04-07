using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

/// <summary>Command cập nhật sản phẩm.</summary>
public record UpdateProductCommand(
    /// <summary>Mã sản phẩm cần cập nhật.</summary>
    Guid Id,
    /// <summary>Tên sản phẩm.</summary>
    string Name,
    /// <summary>Giá sản phẩm.</summary>
    decimal Price,
    /// <summary>Mã tiền tệ (VND, USD...).</summary>
    string Currency,
    /// <summary>Mã danh mục.</summary>
    Guid CategoryId,
    /// <summary>Mô tả sản phẩm (không bắt buộc).</summary>
    string? Description) : IRequest<Result<Guid>>;
