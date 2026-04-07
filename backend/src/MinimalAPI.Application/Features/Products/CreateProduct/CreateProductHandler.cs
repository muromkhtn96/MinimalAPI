using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler(
    ICategoryRepository categoryRepo,
    IProductRepository productRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
        if (category is null)
            return Result<Guid>.Failure("Danh mục không tồn tại.");

        var productName = ProductName.Create(request.Name);
        var productPrice = Money.Create(request.Price, request.Currency);

        var product = Product.Create(productName, productPrice, new CategoryId(request.CategoryId), request.Description);

        productRepo.Add(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}
