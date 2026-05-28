using MediatR;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Application.Features.Customers.DTOs;
using MinimalAPI.Domain.Enums;

namespace MinimalAPI.Application.Features.Customers.UpdateCustomer;

/// <summary>
/// Cập nhật thông tin khách hàng
/// </summary>
/// <param name="Id"></param>
/// <param name="FullName"></param>
/// <param name="Phone"></param>
/// <param name="TaxCode"></param>
/// <param name="Gender"></param>
/// <param name="DateOfBirth"></param>
/// <param name="Address"></param>
/// <param name="Note"></param>
/// <returns></returns>
public sealed record UpdateCustomerCommand(
    Guid Id,
    string FullName,
    string? Phone,
    string? TaxCode,
    Gender? Gender,
    DateTime? DateOfBirth,
    string? Address,
    string? Note) : IRequest<Result<CustomerDto>>;