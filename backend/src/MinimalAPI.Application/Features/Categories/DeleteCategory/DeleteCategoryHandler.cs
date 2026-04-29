using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(
    ICategoryRepository categoryRepo,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork,
    ILogger<DeleteCategoryHandler> logger)
    : IRequestHandler<DeleteCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        logger.LogInformation(
            "Bắt đầu xóa danh mục. Id={Id}",
            request.Id);

        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
        if (category is null)
        {
            logger.LogWarning(
                "Xóa danh mục thất bại vì danh mục không tồn tại. Id={Id}",
                request.Id);

            return Result<Guid>.Failure("Danh mục không tồn tại.");
        }

        var productCount = await db.Products
            .CountAsync(p => p.CategoryId == new CategoryId(request.Id), ct);

        if (productCount > 0)
        {
            logger.LogWarning(
                "Xóa danh mục thất bại vì còn {ProductCount} sản phẩm thuộc danh mục này. Id={Id}",
                productCount,
                request.Id);

            return Result<Guid>.Failure($"Không thể xóa — còn {productCount} sản phẩm thuộc danh mục này.");
        }

        categoryRepo.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(category.Id.Value);
    }
}
