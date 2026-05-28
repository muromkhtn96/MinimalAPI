using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Enums;

namespace MinimalAPI.Application.Features.Customers.CreateCustomer;

/// <summary>
/// Lệnh tạo khách hàng mới.
/// </summary>
/// <param name="FullName"></param>
/// <param name="Phone"></param>
/// <param name="Email"></param>
/// <param name="Type"></param>
/// <param name="TaxCode"></param>
/// <param name="Gender"></param>
/// <param name="DateOfBirth"></param>
/// <param name="Address"></param>
/// <param name="Note"></param>
/// <returns></returns>
public sealed record CreateCustomerCommand(
    string FullName,
    string? Phone,
    string? Email,
    CustomerType Type,
    string? TaxCode,
    Gender? Gender,
    DateTime? DateOfBirth,
    string? Address,
    string? Note) : IRequest<Result<CustomerDto>>;