using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Products.UpdateProductDeactive;

public sealed class UpdateProductDeactiveHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductDeactiveCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductDeactiveCommand request, CancellationToken ct)
    {
        var product = await productRepository.GetByIdAsync(new ProductId(request.ProductId), ct);
        if (product is null)
            return Result<Guid>.Failure("Sản phẩm không tồn tại.");
        
        var deactiveProducts = await productRepository.GetDeactiveProductsAsync(ct);
        if (product.IsActive && deactiveProducts.Count >= 10)
            return Result<Guid>.Failure("Không thể tắt sản phẩm. Đã có 10 sản phẩm đang tắt hoạt động.");


        product.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id.Value);
    }
}
