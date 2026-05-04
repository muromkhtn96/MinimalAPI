using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
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
    
            await unitOfWork.BeginTransactionAsync(ct);

            var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
            if (product is null)
            {
                logger.LogWarning(
                    "Cập nhật sản phẩm thất bại vì sản phẩm không tồn tại. ProductId={ProductId}, ProductName={ProductName}",
                    request.Id,
                    request.Name);
                await unitOfWork.RollbackAsync(ct);
                return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
            }

            var productName = ProductName.Create(request.Name);
            var productPrice = Money.Create(request.Price, request.Currency);

            product.UpdateInfo(productName, new CategoryId(request.CategoryId), request.Description);
            product.UpdatePrice(productPrice);

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation(
                "Cập nhật sản phẩm thành công. ProductId={ProductId}, ProductName={ProductName}, CategoryId={CategoryId}",
                product.Id.Value,
                request.Name,
                request.CategoryId);

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
                "Cập nhật sản phẩm thất bại do lỗi hệ thống. ProductId={ProductId}, ProductName={ProductName}, CategoryId={CategoryId}, Price={Price}, Currency={Currency}",
                request.Id,
                request.Name,
                request.CategoryId,
                request.Price,
                request.Currency);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}

