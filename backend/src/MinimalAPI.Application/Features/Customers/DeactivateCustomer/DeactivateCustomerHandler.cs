using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

using Microsoft.Extensions.Logging;

namespace MinimalAPI.Application.Features.Customers.DeactivateCustomer;

public sealed class DeactivateCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<DeactivateCustomerHandler> logger)
    : IRequestHandler<DeactivateCustomerCommand, Result<CustomerDto>>
{
    /// <summary>
    /// Xử lý yêu cầu vô hiệu hóa khách hàng
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<CustomerDto>> Handle(DeactivateCustomerCommand request, CancellationToken ct)
    {
        var customer = await customerRepository.GetByIdAsync(new CustomerId(request.Id), ct);
        if (customer is null)
        {
            logger.LogWarning("Không tìm thấy khách hàng với Id {Id}", request.Id);
            return Result<CustomerDto>.Failure("Khách hàng không tồn tại");
        }

        if (!customer.IsActive)
        {
            logger.LogInformation("Khách hàng {Id} '{FullName}' đã bị vô hiệu hóa trước đó", customer.Id.Value, customer.FullName);
            return Result<CustomerDto>.Failure("Khách hàng đã bị vô hiệu hóa trước đó");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            customer.Deactivate(); 
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Vô hiệu hóa khách hàng {Id} '{FullName}' thành công", customer.Id.Value, customer.FullName);

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
                customer.UpdateAt
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Vô hiệu hóa khách hàng {Id} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
