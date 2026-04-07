using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Products.CreateProduct;

/// <summary>Command tạo sản phẩm mới.</summary>
public record CreateProductCommand(
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
