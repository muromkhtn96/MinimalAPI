using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
            return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");

        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
        if (category is null)
            return Result<ProductDto>.Failure("Danh mục không tồn tại.");

        var productName = ProductName.Create(request.Name);
        var productPrice = Money.Create(request.Price, request.Currency);

        product.UpdateInfo(productName, new CategoryId(request.CategoryId), request.Description);
        product.UpdatePrice(productPrice);

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
