using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public sealed class UpdateProductDeactiveHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductDeactiveHandler> logger)
    : IRequestHandler<UpdateProductDeactiveCommand, Result<ProductDto>>
{
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

        product.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Đã hủy kích hoạt sản phẩm {ProductId} '{Name}'",
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
}