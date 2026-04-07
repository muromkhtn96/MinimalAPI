namespace MinimalAPI.Application.Features.Categories.DTOs;

/// <summary>DTO trả về thông tin danh mục cho client.</summary>
public record CategoryDto(
    /// <summary>Mã danh mục.</summary>
    Guid Id,
    /// <summary>Tên danh mục.</summary>
    string Name,
    /// <summary>Mô tả danh mục.</summary>
    string? Description,
    /// <summary>Ngày tạo.</summary>
    DateTime CreatedAt);
