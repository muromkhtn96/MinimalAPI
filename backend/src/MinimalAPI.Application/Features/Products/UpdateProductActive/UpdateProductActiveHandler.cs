using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public sealed class UpdateProductActiveHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductActiveCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductActiveCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
            return Result<Guid>.Failure("Sản phẩm không tồn tại.");
        product.SetActive(request.IsActive);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}
