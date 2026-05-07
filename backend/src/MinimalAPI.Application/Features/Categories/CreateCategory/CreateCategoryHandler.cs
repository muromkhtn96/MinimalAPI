using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (await categoryRepo.ExistsByNameAsync(request.Name, ct))
            return Result<CategoryDto>.Failure("Tên danh mục đã tồn tại.");

        var category = Category.Create(request.Name, request.Description);

        categoryRepo.Add(category);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CategoryDto>.Success(new CategoryDto(
            category.Id.Value, 
            category.Name, 
            category.Description,
            category.CreatedAt));
    }
}
