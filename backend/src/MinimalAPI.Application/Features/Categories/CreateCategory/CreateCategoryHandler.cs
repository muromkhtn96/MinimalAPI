using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (await categoryRepo.ExistsByNameAsync(request.Name, ct))
            return Result<Guid>.Failure("Tên danh mục đã tồn tại.");

        var category = Category.Create(request.Name, request.Description);

        categoryRepo.Add(category);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(category.Id.Value);
    }
}
