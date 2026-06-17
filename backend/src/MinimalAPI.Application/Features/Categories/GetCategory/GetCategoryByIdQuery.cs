using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;

namespace MinimalAPI.Application.Features.Categories.GetCategory;
/// <summary>
/// Lấy thông tin danh mục theo ID
/// </summary>
/// <param name="Id"></param>
public record GetCategoryByIdQuery(
    /// <summary>Mã danh mục.</summary>
    Guid Id) : IRequest<Result<CategoryDto>>;
