using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;

namespace MinimalAPI.Application.Features.Categories.GetCategories;

public sealed class GetCategoriesHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryDto>>
{
    public async Task<PagedResult<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var totalCount = await db.Categories.CountAsync(ct);

        var items = await db.Categories
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryDto(
                c.Id.Value,
                c.Name,
                c.Description,
                c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CategoryDto>(items, totalCount, request.Page, request.PageSize);
    }
}
