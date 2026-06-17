using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler(
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ICacheService cacheService,
    ILogger<DeleteProductHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result<ProductDto>>
{
    /// <summary>
    /// Xử lý lệnh xóa sản phẩm
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<ProductDto>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
        {
            logger.LogWarning("Xóa sản phẩm bị từ chối - không tìm thấy sản phẩm {ProductId}", request.Id);
            return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
        }

        var productCode = product.Code;

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            productRepo.Remove(product);
            await unitOfWork.CommitAsync(ct);

            await cacheService.RemoveAsync(CacheKeys.ProductById(request.Id), ct);
            await cacheService.RemoveAsync(CacheKeys.ProductByCode(productCode), ct);

            logger.LogInformation("Đã xóa sản phẩm {ProductId} '{Name}'",
                product.Id.Value, product.Name.Value);

            return Result<ProductDto>.Success(new ProductDto(
                product.Id.Value,
                product.Code,
                product.Name.Value,
                product.Price.Amount,
                product.Price.Currency,
                product.CategoryId.Value,
                product.Category.Name,
                product.Description,
                product.IsActive,
                product.CreatedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Xóa sản phẩm {ProductId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
