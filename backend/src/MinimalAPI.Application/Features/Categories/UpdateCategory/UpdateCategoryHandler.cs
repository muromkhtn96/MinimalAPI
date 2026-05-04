using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCategoryHandler> logger)
    : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        try
            {
            logger.LogInformation(
                "Bắt đầu cập nhật danh mục. Id={Id}, Name={Name}, Description={Description}",
                request.Id,
                request.Name,
                request.Description);

            var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
            if (category is null)
            {
                logger.LogWarning(
                    "Cập nhật danh mục thất bại vì danh mục không tồn tại. Id={Id}",
                    request.Id);

                return Result<CategoryDto>.Failure("Danh mục không tồn tại.");
            }

            category.Update(request.Name, request.Description);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Danh mục được cập nhật thành công. CategoryId={CategoryId}, Name={Name}",
                category.Id.Value,
                request.Name);
            return Result<CategoryDto>.Success(new CategoryDto(
                category.Id.Value,
                category.Name,
                category.Description,
                category.CreatedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Cập nhật danh mục thất bại do lỗi hệ thống. Id={Id}, Name={Name}, Description={Description}",
                request.Id,
                request.Name,
                request.Description);

            throw;
        }
    }
}
