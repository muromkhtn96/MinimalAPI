using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ILogger<DeleteProductHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation(
                "Bắt đầu xóa sản phẩm. ProductId={ProductId}",
                request.Id);

            var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
            if (product is null)
            {
                logger.LogWarning(
                    "Xóa sản phẩm thất bại vì sản phẩm không tồn tại. ProductId={ProductId}",
                    request.Id);

                return Result<Guid>.Failure("Sản phẩm không tồn tại.");
            }

            productRepo.Remove(product);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Xóa sản phẩm thành công. ProductId={ProductId}, ProductName={ProductName}",
                product.Id.Value,
                product.Name.Value);

            return Result<Guid>.Success(product.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Xóa sản phẩm thất bại do lỗi hệ thống. ProductId={ProductId}",
                request.Id);

            throw;
        }
    }
}
