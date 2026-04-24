using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.GetCategories;

public sealed class GetCategoriesHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryDto>>
{
    public async Task<PagedResult<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var totalCount = await categoryRepository.CountAsync(ct);
        var categories = await categoryRepository.GetPagedAsync(request.Page, request.PageSize, ct);

        var items = categories
            .Select(c => new CategoryDto(
                c.Id.Value,
                c.Name,
                c.Description,
                c.CreatedAt))
            .ToList();

        return new PagedResult<CategoryDto>(items, totalCount, request.Page, request.PageSize);
    }
}
