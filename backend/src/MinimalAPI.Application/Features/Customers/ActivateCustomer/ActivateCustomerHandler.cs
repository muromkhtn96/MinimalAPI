using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

using Microsoft.Extensions.Logging;

namespace MinimalAPI.Application.Features.Customers.ActivateCustomer;

public sealed class ActivateCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<ActivateCustomerHandler> logger)
    : IRequestHandler<ActivateCustomerCommand, Result<CustomerDto>>
{
    /// <summary>
    /// Xử lý yêu cầu kích hoạt khách hàng
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<CustomerDto>> Handle(ActivateCustomerCommand request, CancellationToken ct)
    {
        var customer = await customerRepository.GetByIdAsync(new CustomerId(request.Id), ct);
        if (customer is null)
        {
            logger.LogWarning("Không tìm thấy khách hàng {Id} để kích hoạt", request.Id);
            return Result<CustomerDto>.Failure("Khách hàng không tồn tại");
        }

        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            customer.Activate();
            await unitOfWork.CommitAsync(ct);

            logger.LogInformation("Kích hoạt khách hàng {Id} thành công", customer.Id.Value);

            return Result<CustomerDto>.Success( new CustomerDto(
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
            logger.LogError(ex, "Kích hoạt khách hàng {Id} thất bại - đã rollback", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
