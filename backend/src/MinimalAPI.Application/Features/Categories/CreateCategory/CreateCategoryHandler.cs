using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork,
    ILogger<CreateCategoryHandler> logger)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        logger.LogInformation(
            "Bắt đầu tạo danh mục. Name={Name}, Description={Description}",
            request.Name,
            request.Description);

        if (await categoryRepo.ExistsByNameAsync(request.Name, ct))
        {
            logger.LogWarning(
                "Tạo danh mục thất bại vì tên danh mục đã tồn tại. Name={Name}",
                request.Name);

            return Result<Guid>.Failure("Tên danh mục đã tồn tại.");
        }

        var category = Category.Create(request.Name, request.Description);

        categoryRepo.Add(category);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Danh mục được tạo thành công. CategoryId={CategoryId}, Name={Name}",
            category.Id.Value,
            request.Name);

        return Result<Guid>.Success(category.Id.Value);
    }
}
