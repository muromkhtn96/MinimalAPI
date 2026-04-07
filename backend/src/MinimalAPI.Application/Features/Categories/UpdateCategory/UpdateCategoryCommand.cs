using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Categories.UpdateCategory;

/// <summary>Command cập nhật danh mục.</summary>
public record UpdateCategoryCommand(
    /// <summary>Mã danh mục cần cập nhật.</summary>
    Guid Id,
    /// <summary>Tên danh mục.</summary>
    string Name,
    /// <summary>Mô tả danh mục (không bắt buộc).</summary>
    string? Description) : IRequest<Result<Guid>>;
