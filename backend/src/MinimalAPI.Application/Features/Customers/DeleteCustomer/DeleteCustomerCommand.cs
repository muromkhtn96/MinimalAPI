using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.DeleteCustomer;

/// <summary>
/// Xóa khách hàng
/// </summary>
/// <param name="Id"></param>
/// <returns></returns>
public sealed record DeleteCustomerCommand(Guid Id) : IRequest<Result<CustomerDto>>;
