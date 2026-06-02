using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.GetCustomer;

public sealed class GetCustomerHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    /// <summary>
    /// Xử lý yêu cầu lấy thông tin khách hàng theo ID
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var customer = await customerRepository.GetByIdAsync(new CustomerId(request.Id), ct);

        if (customer is null) 
        {
            return Result<CustomerDto>.Failure("Khách hàng không tồn tại.");
        }
        
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
}
