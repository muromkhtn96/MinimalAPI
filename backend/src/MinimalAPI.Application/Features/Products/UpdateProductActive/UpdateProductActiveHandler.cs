using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public sealed class UpdateProductActiveHandler(
    IProductRepository productRepo,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<UpdateProductActiveHandler> logger)
    : IRequestHandler<UpdateProductActiveCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductActiveCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
        {
            logger.LogWarning("Kích hoạt sản phẩm bị từ chối - không tìm thấy sản phẩm {ProductId}", request.Id);
            return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
        }

        var activeProducts = await productRepo.GetActiveProductsAsync(ct);
        if (!product.IsActive && activeProducts.Count >= 10)
        {
            logger.LogWarning("Không thể kích hoạt sản phẩm {ProductId} - đã đạt giới hạn 10 sản phẩm đang hoạt động",
            request.Id);
            return Result<ProductDto>.Failure("Không thể kích hoạt sản phẩm. Đã có 10 sản phẩm đang hoạt động.");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            product.Activate();
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Đã kích hoạt sản phẩm {ProductId} '{Name}'",
                    product.Id.Value, product.Name.Value);

            return Result<ProductDto>.Success(new ProductDto(
                product.Id.Value,
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
            logger.LogError(ex, "Kích hoạt sản phẩm {ProductId} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
