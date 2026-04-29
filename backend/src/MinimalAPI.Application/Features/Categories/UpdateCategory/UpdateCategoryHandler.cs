using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCategoryHandler> logger)
    : IRequestHandler<UpdateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateCategoryCommand request, CancellationToken ct)
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

                return Result<Guid>.Failure("Danh mục không tồn tại.");
            }

            category.Update(request.Name, request.Description);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Danh mục được cập nhật thành công. CategoryId={CategoryId}, Name={Name}",
                category.Id.Value,
                request.Name);
            return Result<Guid>.Success(category.Id.Value);
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
