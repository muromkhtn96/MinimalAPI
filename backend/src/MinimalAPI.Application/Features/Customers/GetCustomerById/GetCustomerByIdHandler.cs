using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Categories.DTOs;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Entities;
using MinimalAPI.Domain.Interfaces;

namespace MinimalAPI.Application.Features.Customers.GetCustomer;

public sealed class GetCustomerHandler(
    ICustomerRepository customerRepository,
    HybridCache hybridCache)
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
        var cacheKey = CacheKeys.CustomerById(request.Id);

        var dto = await hybridCache.GetOrCreateAsync<CustomerDto?>(
            cacheKey,
            async token =>
            {
                var customerId = new CustomerId(request.Id);
                var customer = await customerRepository.GetByIdAsync(customerId, token);

                if (customer is null) 
                {
                    return null;
                }

                return new CustomerDto(
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
                    customer.UpdateAt);
            },
            cancellationToken: ct);

        if (dto is null)
        {
            return Result<CustomerDto>.Failure("Không tìm thấy khách hàng.");
        }

        return Result<CustomerDto>.Success(dto);
    }
}