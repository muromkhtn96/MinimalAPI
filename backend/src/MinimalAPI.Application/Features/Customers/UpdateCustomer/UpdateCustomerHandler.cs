using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.UpdateCustomer;
public sealed class UpdateCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWorkManager unitOfWorkManager,
    HybridCache hybridCache,
    ILogger<UpdateCustomerHandler> logger)
    : IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await customerRepository.GetByIdAsync(new CustomerId(request.Id), ct);
        if (customer is null) 
        {
            logger.LogWarning("Không tìm thấy khách hàng {Id} để cập nhật", request.Id);
            return Result<CustomerDto>.Failure("Khách hàng không tồn tại.");
        }

        if (!string.IsNullOrWhiteSpace(request.Phone) && customer.Phone != request.Phone)
        {
            var phoneExists = await customerRepository.ExistsByPhoneAsync(request.Phone, ct);
            if (phoneExists) return Result<CustomerDto>.Failure("Số điện thoại đã được sử dụng.");
        }

        var customerCode = customer.Code;
        customer.UpdateInfo(
            request.FullName, 
            request.Phone, 
            request.TaxCode,
            request.Gender, 
            request.DateOfBirth, 
            request.Address, 
            request.Note);

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            await unitOfWork.CommitAsync(ct);

            await hybridCache.RemoveAsync(CacheKeys.CustomerById(request.Id), ct);
            await hybridCache.RemoveAsync(CacheKeys.CustomerByCode(customerCode), ct);

            logger.LogInformation("Cập nhật khách hàng {Code} thành công", customer.Code);

            return Result<CustomerDto>.Success(new CustomerDto(
                customer.Id.Value, 
                customer.Code, 
                customer.FullName,
                customer.Email, 
                customer.Phone, 
                customer.Type,
                customer.TaxCode, 
                customer.Gender, 
                customer.DateOfBirth,
                customer.Address, 
                customer.Note,
                customer.IsActive, 
                customer.CreatedAt, 
                customer.UpdateAt));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cập nhật khách hàng {Id} thất bại", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}