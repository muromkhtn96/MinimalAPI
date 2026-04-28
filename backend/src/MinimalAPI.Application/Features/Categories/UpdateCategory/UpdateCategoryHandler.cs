using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.Id), ct);
        if (category is null)
            return Result<Guid>.Failure("Danh mục không tồn tại.");

        category.Update(request.Name, request.Description);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(category.Id.Value);
    }
}
