using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductActive;

public sealed class UpdateProductActiveHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductActiveHandler> logger)
    : IRequestHandler<UpdateProductActiveCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        UpdateProductActiveCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Bắt đầu cập nhật trạng thái sản phẩm. ProductId={ProductId}, IsActive={IsActive}",
                request.Id,
                request.IsActive);

            var product = await productRepo.GetByIdAsync(new ProductId(request.Id), cancellationToken);
            if (product is null)
            {
                logger.LogWarning(
                    "Cập nhật trạng thái sản phẩm thất bại vì sản phẩm không tồn tại. ProductId={ProductId}, IsActive={IsActive}",
                    request.Id,
                    request.IsActive);

                return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            product.SetActive(request.IsActive);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

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
                "Cập nhật trạng thái sản phẩm thất bại do lỗi hệ thống. ProductId={ProductId}, IsActive={IsActive}",
                request.Id,
                request.IsActive);

            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
