using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation(
                "Bắt đầu cập nhật sản phẩm. ProductId={ProductId}, ProductName={ProductName}, CategoryId={CategoryId}, Price={Price}, Currency={Currency}",
                request.Id,
                request.Name,
                request.CategoryId,
                request.Price,
                request.Currency);

            var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
            if (product is null)
            {
                logger.LogWarning(
                    "Cập nhật sản phẩm thất bại vì sản phẩm không tồn tại. ProductId={ProductId}, ProductName={ProductName}",
                    request.Id,
                    request.Name);

                return Result<Guid>.Failure("Sản phẩm không tồn tại.");
            }

            var productName = ProductName.Create(request.Name);
            var productPrice = Money.Create(request.Price, request.Currency);

            product.UpdateInfo(productName, new CategoryId(request.CategoryId), request.Description);
            product.UpdatePrice(productPrice);

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Cập nhật sản phẩm thành công. ProductId={ProductId}, ProductName={ProductName}, CategoryId={CategoryId}",
                product.Id.Value,
                request.Name,
                request.CategoryId);

            return Result<Guid>.Success(product.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Cập nhật sản phẩm thất bại do lỗi hệ thống. ProductId={ProductId}, ProductName={ProductName}, CategoryId={CategoryId}, Price={Price}, Currency={Currency}",
                request.Id,
                request.Name,
                request.CategoryId,
                request.Price,
                request.Currency);

            throw;
        }
    }
}
