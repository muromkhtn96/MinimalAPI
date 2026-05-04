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
        try
        {
            logger.LogInformation(
                "Bắt đầu tắt hoạt động sản phẩm. ProductId={ProductId}",
                request.ProductId);

            var product = await productRepository.GetByIdAsync(new ProductId(request.ProductId), ct);
            if (product is null)
            {
                logger.LogWarning(
                    "Tắt hoạt động sản phẩm thất bại vì sản phẩm không tồn tại. ProductId={ProductId}",
                    request.ProductId);

                return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
            }

            await unitOfWork.BeginTransactionAsync(ct);

            var deactiveProducts = await productRepository.GetDeactiveProductsAsync(ct);
            if (product.IsActive && deactiveProducts.Count >= 10)
            {
                logger.LogWarning(
                    "Tắt hoạt động sản phẩm thất bại vì đã có {DeactiveProductCount} sản phẩm đang tắt hoạt động. ProductId={ProductId}",
                    deactiveProducts.Count,
                    request.ProductId);
                await unitOfWork.RollbackAsync(ct);
                return Result<ProductDto>.Failure("Không thể tắt sản phẩm. Đã có 10 sản phẩm đang tắt hoạt động.");
            }
            

            product.Deactivate();
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Tắt hoạt động sản phẩm thành công. ProductId={ProductId}, IsActive={IsActive}",
                product.Id.Value,
                product.IsActive);

            var productDto = new ProductDto(
                product.Id.Value,
                product.Name.Value,
                product.Price.Amount,
                product.Price.Currency,
                product.CategoryId.Value,
                product.Category.Name,
                product.Description,
                product.IsActive,
                product.CreatedAt);
            return Result<ProductDto>.Success(productDto);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Tắt hoạt động sản phẩm thất bại do lỗi hệ thống. ProductId={ProductId}",
                request.ProductId);

            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
