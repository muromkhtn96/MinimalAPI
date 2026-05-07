using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Products.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler(
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ILogger<DeleteProductHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(new ProductId(request.Id), ct);
        if (product is null)
        {
            logger.LogWarning("Xóa sản phẩm bị từ chối - không tìm thấy sản phẩm {ProductId}", request.Id);
            return Result<ProductDto>.Failure("Sản phẩm không tồn tại.");
        }

        productRepo.Remove(product);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Đã xóa sản phẩm {ProductId} '{Name}' - trạng thái trước đó: {Status}",
            product.Id.Value, product.Name.Value,
            product.IsActive ? "đang hoạt động" : "không hoạt động");

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