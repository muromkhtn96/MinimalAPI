using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler(
    ICategoryRepository categoryRepo,
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ICodeGenerator codeGenerator,
    ILogger<CreateProductHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    /// <summary>
    /// Xử lý lệnh tạo sản phẩm mới
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
        if (category is null)
        {
            logger.LogWarning("Tạo sản phẩm bị từ chối - không tìm thấy danh mục {CategoryId}", request.CategoryId);
            return Result<ProductDto>.Failure("Danh mục không tồn tại.");
        }

        if (await productRepo.ExistsByNameAsync(request.Name, ct))
        {
            logger.LogWarning("Tạo sản phẩm bị từ chối - tên '{Name}' đã tồn tại", request.Name);
            return Result<ProductDto>.Failure("Tên sản phẩm đã tồn tại.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            var code = await codeGenerator.NextAsync("SP", 5, ct);

            var product = Product.Create(
                code,
                ProductName.Create(request.Name),
                Money.Create(request.Price, request.Currency),
                new CategoryId(request.CategoryId),
                request.Description);

            productRepo.Add(product);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Sản phẩm {ProductId} '{Name}' đã được tạo trong danh mục {CategoryId}",
                product.Id.Value, product.Name.Value, product.CategoryId.Value);

            return Result<ProductDto>.Success(new ProductDto(
                product.Id.Value,
                product.Code,
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
            logger.LogError(ex, "Tạo sản phẩm '{Name}' thất bại - đã rollback", request.Name);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}