using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;

namespace MinimalAPI.Application.Features.Products.GetProducts;

/// <summary>Query lấy danh sách sản phẩm có phân trang và tìm kiếm.</summary>
public record GetProductsQuery(
    /// <summary>Trang hiện tại (mặc định 1).</summary>
    int Page = 1,
    /// <summary>Số item mỗi trang (mặc định 10).</summary>
    int PageSize = 10,
    /// <summary>Từ khóa tìm kiếm theo tên (không bắt buộc).</summary>
    string? Search = null) : IRequest<PagedResult<ProductDto>>;
