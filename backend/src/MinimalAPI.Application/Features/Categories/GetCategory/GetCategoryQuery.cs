using MediatR;
using MinimalAPI.Application.Features.Categories.DTOs;

namespace MinimalAPI.Application.Features.Categories.GetCategory;

/// <summary>Query lấy chi tiết danh mục theo Id.</summary>
public record GetCategoryQuery(
    /// <summary>Mã danh mục cần lấy.</summary>
    Guid Id) : IRequest<CategoryDto?>;
