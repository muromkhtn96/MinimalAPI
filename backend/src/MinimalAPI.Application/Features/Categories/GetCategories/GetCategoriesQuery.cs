using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;

namespace MinimalAPI.Application.Features.Categories.GetCategories;

/// <summary>Query lấy danh sách danh mục có phân trang.</summary>
public record GetCategoriesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<CategoryDto>>;
