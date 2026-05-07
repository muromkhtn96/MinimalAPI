using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
        if (category is null)
            return Result<CategoryDto>.Failure("Danh mục không tồn tại.");

        if (await categoryRepo.ExistsByNameAsync(request.Name, new CategoryId(request.Id), ct))
            return Result<CategoryDto>.Failure("Tên danh mục đã tồn tại.");

        category.Update(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id.Value,
            category.Name,
            category.Description,
            category.CreatedAt));
    }
}
