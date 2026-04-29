using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public sealed class UpdateProductActiveHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductActiveHandler> logger)
    : IRequestHandler<UpdateProductActiveCommand, Result<Guid>>
{
        public async Task<Result<Guid>> Handle(UpdateProductActiveCommand request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation(
                "Bắt đầu cập nhật trạng thái sản phẩm. ProductId={ProductId}, IsActive={IsActive}",
                request.Id,
                request.IsActive);

            var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
            if (product is null)
            {
                logger.LogWarning(
                    "Cập nhật trạng thái sản phẩm thất bại vì sản phẩm không tồn tại. ProductId={ProductId}, IsActive={IsActive}",
                    request.Id,
                    request.IsActive);

                return Result<Guid>.Failure("Sản phẩm không tồn tại.");
            }

            var activeProducts = await productRepo.GetActiveProductsAsync(ct);
            if (request.IsActive && !product.IsActive && activeProducts.Count >= 10)
            {
                logger.LogWarning(
                    "Cập nhật trạng thái sản phẩm thất bại vì đã có {ActiveProductCount} sản phẩm đang hoạt động. ProductId={ProductId}",
                    activeProducts.Count,
                    request.Id);

                return Result<Guid>.Failure("Không thể kích hoạt sản phẩm. Đã có 10 sản phẩm đang hoạt động.");
            }

            product.SetActive(request.IsActive);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Cập nhật trạng thái sản phẩm thành công. ProductId={ProductId}, IsActive={IsActive}",
                product.Id.Value,
                request.IsActive);

            return Result<Guid>.Success(product.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Cập nhật trạng thái sản phẩm thất bại do lỗi hệ thống. ProductId={ProductId}, IsActive={IsActive}",
                request.Id,
                request.IsActive);

            throw;
        }
    }
}
