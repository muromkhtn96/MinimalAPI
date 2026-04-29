using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
using MinimalAPI.Domain.ValueObjects;

namespace MinimalAPI.Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler(
    ICategoryRepository categoryRepo,
    IProductRepository productRepo,
    IUnitOfWork unitOfWork,
    ILogger<CreateProductHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation(
                "Bắt đầu tạo sản phẩm. ProductName={ProductName}, CategoryId={CategoryId}, Price={Price}, Currency={Currency}",
                request.Name,
                request.CategoryId,
                request.Price,
                request.Currency);

            var category = await categoryRepo.GetByIdAsync(new CategoryId(request.CategoryId), ct);
            if (category is null)
            {
                logger.LogWarning(
                    "Tạo sản phẩm thất bại vì danh mục không tồn tại. CategoryId={CategoryId}",
                    request.CategoryId);

                return Result<Guid>.Failure("Danh mục không tồn tại.");
            }

            var productName = ProductName.Create(request.Name);
            var productPrice = Money.Create(request.Price, request.Currency);

            var product = Product.Create(
                productName,
                productPrice,
                new CategoryId(request.CategoryId),
                request.Description);

            productRepo.Add(product);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Sản phẩm được tạo thành công. ProductId={ProductId}, Name={ProductName}",
                product.Id.Value,
                request.Name);

            return Result<Guid>.Success(product.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Tạo sản phẩm thất bại. Name={ProductName}, CategoryId={CategoryId}, Price={Price}, Currency={Currency}",
                request.Name,
                request.CategoryId,
                request.Price,
                request.Currency);

            throw;
        }
    }
}
