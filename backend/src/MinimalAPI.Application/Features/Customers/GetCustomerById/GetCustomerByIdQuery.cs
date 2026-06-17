using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.GetCustomer;
/// <summary>
/// Lấy thông tin khách hàng theo ID
/// </summary>
/// <param name="Id"></param>
/// <returns></returns>
public sealed record GetCustomerByIdQuery(
    /// <summary>Mã khách hàng.</summary>
    Guid Id) : IRequest<Result<CustomerDto>>;