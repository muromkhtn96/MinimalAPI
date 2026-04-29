using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public sealed class UpdateProductDeactiveHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductDeactiveHandler> logger)
    : IRequestHandler<UpdateProductDeactiveCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductDeactiveCommand request, CancellationToken ct)
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

                return Result<Guid>.Failure("Sản phẩm không tồn tại.");
            }

            var deactiveProducts = await productRepository.GetDeactiveProductsAsync(ct);
            if (product.IsActive && deactiveProducts.Count >= 10)
            {
                logger.LogWarning(
                    "Tắt hoạt động sản phẩm thất bại vì đã có {DeactiveProductCount} sản phẩm đang tắt hoạt động. ProductId={ProductId}",
                    deactiveProducts.Count,
                    request.ProductId);

                return Result<Guid>.Failure("Không thể tắt sản phẩm. Đã có 10 sản phẩm đang tắt hoạt động.");
            }

            product.Deactivate();
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Tắt hoạt động sản phẩm thành công. ProductId={ProductId}, IsActive={IsActive}",
                product.Id.Value,
                product.IsActive);

            return Result<Guid>.Success(product.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Tắt hoạt động sản phẩm thất bại do lỗi hệ thống. ProductId={ProductId}",
                request.ProductId);

            throw;
        }
    }
}
