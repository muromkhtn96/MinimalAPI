using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;

namespace MinimalAPI.Application.Features.Customers.GetCustomerByCode;
/// <summary>
/// Lấy thông tin khách hàng theo mã khách hàng
/// </summary>
/// <param name="Code"></param> <summary>
/// 
/// </summary>
/// <param name="Code"></param>
/// <returns></returns>
public sealed record GetCustomerByCodeQuery(string Code) : IRequest<Result<CustomerDto>>;