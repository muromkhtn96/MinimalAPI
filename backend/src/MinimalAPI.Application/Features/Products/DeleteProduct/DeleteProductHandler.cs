using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
            return Result<Guid>.Failure("Sản phẩm không tồn tại.");

        productRepo.Remove(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}
