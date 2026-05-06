using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public sealed record UpdateProductDeactiveHandler(
    IProductRepository ProductRepository,
    IUnitOfWork UnitOfWork)
    : IRequestHandler<UpdateProductDeactiveCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductDeactiveCommand request, CancellationToken ct)
    {
        var product = await ProductRepository.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
            return Result<Guid>.Failure("Sản phẩm không tồn tại.");

        product.Deactivate();
        await UnitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}