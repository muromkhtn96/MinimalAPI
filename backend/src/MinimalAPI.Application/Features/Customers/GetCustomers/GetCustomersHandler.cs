using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.GetCustomers;

public sealed class GetCustomersHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomersQuery, Result<List<CustomersDto>>> 
{
    /// <summary>
    /// Xử lý truy vấn lấy danh sách khách hàng.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Result<List<CustomersDto>>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var customers = await customerRepository.GetAllAsync(
            request.Keyword,
            request.Page,
            request.PageSize,
            ct);

        var customer = customers
            .Select(customer => new CustomersDto(
                customer.Id.Value,
                customer.Code,
                customer.FullName,
                customer.IsActive))
                .ToList();

        return Result<List<CustomersDto>>.Success(customer);
    }
}
