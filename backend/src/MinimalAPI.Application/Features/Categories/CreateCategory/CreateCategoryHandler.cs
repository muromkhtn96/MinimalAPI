using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork,
    ILogger<CreateCategoryHandler> logger)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (await categoryRepo.ExistsByNameAsync(request.Name, ct))
        {
            logger.LogWarning("Tạo danh mục bị từ chối - tên '{Name}' đã tồn tại", request.Name);
            return Result<CategoryDto>.Failure("Tên danh mục đã tồn tại.");
        }

        var category = Category.Create(request.Name, request.Description);

        categoryRepo.Add(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Danh mục {CategoryId} '{Name}' đã được tạo",
            category.Id, category.Name);

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id.Value,
            category.Name,
            category.Description,
            category.CreatedAt));
    }
}