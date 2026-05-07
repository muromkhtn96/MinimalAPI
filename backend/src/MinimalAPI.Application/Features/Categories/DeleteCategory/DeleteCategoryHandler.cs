using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(
    ICategoryRepository categoryRepo,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork,
    ILogger<DeleteCategoryHandler> logger)
    : IRequestHandler<DeleteCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
        if (category is null)
        {
            logger.LogWarning("Xóa danh mục bị từ chối - không tìm thấy danh mục {CategoryId}", request.Id);
            return Result<CategoryDto>.Failure("Danh mục không tồn tại.");
        }

        var productCount = await db.Products
            .CountAsync(p => p.CategoryId == new CategoryId(request.Id), ct);

        if (productCount > 0)
        {
            logger.LogWarning("Không thể xóa danh mục {CategoryId} '{Name}' - đang có {ProductCount} sản phẩm",
                category.Id.Value, category.Name, productCount);
            return Result<CategoryDto>.Failure($"Không thể xóa — còn {productCount} sản phẩm thuộc danh mục này.");
        }

        categoryRepo.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Đã xóa thành công danh mục {CategoryId} '{Name}'",
                category.Id.Value, category.Name);

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id.Value,
            category.Name,
            category.Description,
            category.CreatedAt));
    }
}