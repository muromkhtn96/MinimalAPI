using MinimalAPI.Domain.Enums;

namespace MinimalAPI.Application.Features.Customers.DTOs;

public record CustomerDto(
    Guid Id,
    string Code,
    string FullName,
    string? Email,
    string? Phone,
    CustomerType Type,
    string? TaxCode,
    Gender? Gender,
    DateTime? DateOfBirth,
    string? Address,
    string? Note,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdateAt
);