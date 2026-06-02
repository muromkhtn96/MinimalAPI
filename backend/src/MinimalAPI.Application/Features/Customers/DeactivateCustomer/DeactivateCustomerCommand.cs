using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.DeactivateCustomer;
/// <summary>
/// Lệnh vô hiệu hóa khách hàng
/// </summary>
/// <param name="Id"></param> <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <returns></returns>
public record DeactivateCustomerCommand(Guid Id) : IRequest<Result<CustomerDto>>;