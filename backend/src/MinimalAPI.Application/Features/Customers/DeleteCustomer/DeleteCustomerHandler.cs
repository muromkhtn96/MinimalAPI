using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;
namespace MinimalAPI.Application.Features.Customers.DeleteCustomer;
public sealed class DeleteCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<DeleteCustomerHandler> logger)
    : IRequestHandler<DeleteCustomerCommand, Result<CustomerDto>>
{
    /// <summary>
    /// Xử lý yêu cầu xóa khách hàng
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<CustomerDto>> Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var customer = await customerRepository.GetByIdAsync(new CustomerId(request.Id), ct);
        if (customer is null)
        {
            logger.LogWarning("Xóa khách hàng bị từ chối - Không tìm thấy khách hàng {CustomerId}", request.Id);
            return Result<CustomerDto>.Failure("Khách hàng không tồn tại.");
        }
        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            customerRepository.Remove(customer);
            await unitOfWork.CommitAsync(ct);
            logger.LogInformation("Đã xóa khách hàng {CustomerId} '{FullName}' - trạng thái trước đó: {Status}",
                customer.Id.Value, 
                customer.FullName, 
                customer.IsActive ? "Đã kích hoạt" : "Hủy kích hoạt");
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
            logger.LogError(ex, "Xóa khách hàng {CustomerId} thất bại - đã roll back", request.Id);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
}
