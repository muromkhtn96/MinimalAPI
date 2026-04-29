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
        try
        {
            logger.LogInformation(
                "Bắt đầu xóa danh mục. CategoryId={CategoryId}",
                request.Id);

            var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
            if (category is null)
            {
                logger.LogWarning(
                    "Xóa danh mục thất bại vì danh mục không tồn tại. CategoryId={CategoryId}",
                    request.Id);

                return Result<Guid>.Failure("Danh mục không tồn tại.");
            }

            var productCount = await db.Products
                .CountAsync(p => p.CategoryId == new CategoryId(request.Id), ct);

            if (productCount > 0)
            {
                logger.LogWarning(
                    "Xóa danh mục thất bại vì còn {ProductCount} sản phẩm thuộc danh mục này. CategoryId={CategoryId}",
                    productCount,
                    request.Id);

                return Result<Guid>.Failure($"Không thể xóa danh mục vì còn {productCount} sản phẩm thuộc danh mục này.");
            }

            categoryRepo.Remove(category);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Xóa danh mục thành công. CategoryId={CategoryId}, CategoryName={CategoryName}",
                category.Id.Value,
                category.Name);

            return Result<Guid>.Success(category.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Xóa danh mục thất bại do lỗi hệ thống. CategoryId={CategoryId}",
                request.Id);

            throw;
        }
    }
}
