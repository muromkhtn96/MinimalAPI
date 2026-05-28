using MediatR;
using Microsoft.Extensions.Logging;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.CreateCustomer;

public sealed class CreateCustomerHandler(
    ICustomerRepository customerRepository,
    ICodeGenerator codeGenerator,
    IUnitOfWorkManager unitOfWorkManager,
    ILogger<CreateCustomerHandler> logger)
    : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    /// <summary>
    /// Xử lý yêu cầu tạo khách hàng mới
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
   public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        // Chỉ check DB nếu user có nhập số điện thoại
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var exist = await customerRepository.ExistsByPhoneAsync(request.Phone, ct);
            if (exist)
            {
                return Result<CustomerDto>.Failure("Số điện thoại đã tồn tại trong hệ thống.");
            }
        }

        // Sinh mã khách hàng tự động (VD: KH00001)
        var code = await codeGenerator.NextAsync("KH", 5, ct);
        
        await using var unitOfWork = await unitOfWorkManager.NewUnitOfWorkAsync(ct);
        try
        {
            var customer = Customer.Create(
                code,
                request.FullName,
                request.Email,
                request.Phone,     
                request.Type,
                request.TaxCode,
                request.Gender,
                request.DateOfBirth,
                request.Address,
                request.Note);

            customerRepository.Add(customer);
            await unitOfWork.CommitAsync(ct);

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
            // Cập nhật lại câu log báo lỗi theo Số điện thoại thay vì Email
            logger.LogError(ex, "Tạo khách hàng SĐT '{Phone}' thất bại - đã rollback, {FullName}", request.Phone, request.FullName);
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    } 
}