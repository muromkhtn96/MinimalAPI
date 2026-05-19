using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<UpdateProductHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
        {
            logger.LogWarning("Cập nhật sản phẩm bị từ chối - không tìm thấy sản phẩm {ProductId}", request.Id);
            return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
        }

        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
        if (category is null)
        {
            logger.LogWarning("Cập nhật sản phẩm bị từ chối - không tìm thấy danh mục {CategoryId}", request.CategoryId);
            return Result<ProductDto>.Failure("Danh mục không tồn tại.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            var productName  = ProductName.Create(request.Name);
            var productPrice = Money.Create(request.Price, request.Currency);

            product.UpdateInfo(productName, new CategoryId(request.CategoryId), request.Description);
            product.UpdatePrice(productPrice);

            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Sản phẩm {ProductId} đã được cập nhật - tên: '{Name}', giá: {Price} {Currency}",
                product.Id.Value, product.Name.Value, product.Price.Amount, product.Price.Currency);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Cập nhật sản phẩm {ProductId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
