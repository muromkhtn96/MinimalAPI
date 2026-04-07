using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;

namespace MinimalAPI.Application.Features.Categories.GetCategories;

/// <summary>Query lấy danh sách danh mục có phân trang.</summary>
public record GetCategoriesQuery(
    /// <summary>Trang hiện tại (mặc định 1).</summary>
    int Page = 1,
    /// <summary>Số item mỗi trang (mặc định 20).</summary>
    int PageSize = 20) : IRequest<PagedResult<CategoryDto>>;
