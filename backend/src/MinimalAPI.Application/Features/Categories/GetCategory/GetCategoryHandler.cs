using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.GetCategory;

public sealed class GetCategoryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoryQuery, CategoryDto?>
{
    public async Task<CategoryDto?> Handle(GetCategoryQuery request, CancellationToken ct)
    {
        var categoryId = new CategoryId(request.Id);

        return await categoryRepository.GetByIdAsync(categoryId, ct) is Category category
            ? new CategoryDto(
                category.Id.Value,
                category.Name,
                category.Description,
                category.CreatedAt)
            : null;
    }
}
