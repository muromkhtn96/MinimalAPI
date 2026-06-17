using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ICacheService cacheService,
    ILogger<UpdateCategoryHandler> logger)
    : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
        if (category is null)
        {
            logger.LogWarning("Cập nhật danh mục bị từ chối - không tìm thấy danh mục {CategoryId}", request.Id);
            return Result<CategoryDto>.Failure("Danh mục không tồn tại.");
        }

        if (await categoryRepo.ExistsByNameAsync(request.Name, new CategoryId(request.Id), ct))
        {
            logger.LogWarning("Cập nhật danh mục bị từ chối - tên '{Name}' đã tồn tại", request.Name);
            return Result<CategoryDto>.Failure("Tên danh mục đã tồn tại.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            category.Update(request.Name, request.Description);
            await unitOfWork.CommitAsync(ct);

            await cacheService.RemoveAsync(CacheKeys.CategoryById(category.Id.Value), ct);

            logger.LogInformation("Danh mục {CategoryId} đã được cập nhật - tên mới: '{Name}'",
                category.Id.Value, category.Name);

            return Result<CategoryDto>.Success(new CategoryDto(
                category.Id.Value,
                category.Name,
                category.Description,
                category.CreatedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cập nhật danh mục {CategoryId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
