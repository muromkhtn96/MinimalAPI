using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.GetCategory;

public sealed class GetCategoryHandler(
    ICategoryRepository categoryRepository,
    HybridCache hybridCache)
    : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        var cacheKey = CacheKeys.CategoryById(request.Id);

        var dto = await hybridCache.GetOrCreateAsync<CategoryDto?>(
            cacheKey,
            async token =>
            {
                var categoryId = new CategoryId(request.Id);
                var category = await categoryRepository.GetByIdAsync(categoryId, token);

                if (category is null)
                {
                    return null;
                }

                return new CategoryDto(
                    category.Id.Value,
                    category.Name,
                    category.Description,
                    category.CreatedAt);
            },
            cancellationToken: ct);

        if (dto is null)
        {
            return Result<CategoryDto>.Failure("Không tìm thấy danh mục.");
        }

        return Result<CategoryDto>.Success(dto);
    }
}
