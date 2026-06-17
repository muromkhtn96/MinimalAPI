using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public sealed class UpdateProductDeactiveHandler(
    IProductRepository productRepository,
    IUnitOfWorkManager unitOfWorkManager,
    ICacheService cacheService,
    ILogger<UpdateProductDeactiveHandler> logger)
    : IRequestHandler<UpdateProductDeactiveCommand, Result<ProductDto>>
{
    /// <summary>
    /// Xử lý lệnh cập nhật trạng thái không hoạt động của sản phẩm.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<ProductDto>> Handle(UpdateProductDeactiveCommand request, CancellationToken ct)
    {
        var product = await productRepository.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
        {
            logger.LogWarning("Hủy kích hoạt sản phẩm bị từ chối - không tìm thấy sản phẩm {ProductId}", request.Id);
            return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
        }

        var deactiveProducts = await productRepository.GetDeactiveProductsAsync(ct);
        if (product.IsActive && deactiveProducts.Count >= 10)
        {
            logger.LogWarning("Không thể hủy kích hoạt sản phẩm {ProductId} - đã đạt giới hạn 10 sản phẩm không hoạt động",
            request.Id);
            return Result<ProductDto>.Failure("Không thể tắt sản phẩm. Đã có 10 sản phẩm đang tắt hoạt động.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            product.Deactivate();
            await unitOfWork.CommitAsync(ct);

            await cacheService.RemoveAsync(CacheKeys.ProductById(product.Id.Value), ct);
            await cacheService.RemoveAsync(CacheKeys.ProductByCode(product.Code), ct);

            logger.LogInformation("Đã hủy kích hoạt sản phẩm {ProductId} '{Name}'",
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
            logger.LogError(ex, "Hủy kích hoạt sản phẩm {ProductId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
