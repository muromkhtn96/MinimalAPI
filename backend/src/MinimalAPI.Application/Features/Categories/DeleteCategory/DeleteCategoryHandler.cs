using MediatR;
using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Application.Features.Categories.DTOs;

namespace MinimalAPI.Application.Features.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(
    ICategoryRepository categoryRepo,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
        if (category is null)
            return Result<CategoryDto>.Failure("Danh mục không tồn tại.");

        var productCount = await db.Products
            .CountAsync(p => p.CategoryId == new CategoryId(request.Id), ct);

        if (productCount > 0)
            return Result<CategoryDto>.Failure($"Không thể xóa — còn {productCount} sản phẩm thuộc danh mục này.");

        categoryRepo.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id.Value, 
            category.Name, 
            category.Description,
            category.CreatedAt));
    }
}
