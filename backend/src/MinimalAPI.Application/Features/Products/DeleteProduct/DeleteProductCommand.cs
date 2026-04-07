using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Products.DeleteProduct;

/// <summary>Command xóa sản phẩm.</summary>
public record DeleteProductCommand(
    /// <summary>Mã sản phẩm cần xóa.</summary>
    Guid Id) : IRequest<Result<Guid>>;
