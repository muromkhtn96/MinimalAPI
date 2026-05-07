using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler(
    ICategoryRepository categoryRepo,
    IProductRepository productRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
        if (category is null)
            return Result<ProductDto>.Failure("Danh mục không tồn tại.");

        if (await productRepo.ExistsByNameAsync(request.Name, ct))
            return Result<ProductDto>.Failure("Tên sản phẩm đã tồn tại.");

        var product = Product.Create(
            ProductName.Create(request.Name),
            Money.Create(request.Price, request.Currency),
            new CategoryId(request.CategoryId),
            request.Description);

        productRepo.Add(product);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<ProductDto>.Success(new ProductDto(
            product.Id.Value,
            product.Name.Value,
            product.Price.Amount,
            product.Price.Currency,
            product.CategoryId.Value,
            category.Name,
            product.Description,
            product.IsActive,
            product.CreatedAt));             
    }
}
