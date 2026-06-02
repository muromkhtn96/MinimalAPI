using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.ActivateCustomer;
/// <summary>
/// Lệnh kích hoạt khách hàng
/// </summary>
/// <param name="Id"></param>
/// <returns></returns>
public sealed record ActivateCustomerCommand(Guid Id) : IRequest<Result<CustomerDto>>;