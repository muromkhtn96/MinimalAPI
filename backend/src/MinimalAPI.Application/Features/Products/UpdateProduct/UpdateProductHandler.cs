using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
            return Result<Guid>.Failure("Sản phẩm không tồn tại.");

        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
        if (category is null)
            return Result<Guid>.Failure("Danh mục không tồn tại.");

        var productName = ProductName.Create(request.Name);
        var productPrice = Money.Create(request.Price, request.Currency);

        product.UpdateInfo(productName, new CategoryId(request.CategoryId), request.Description);
        product.UpdatePrice(productPrice);

        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}
