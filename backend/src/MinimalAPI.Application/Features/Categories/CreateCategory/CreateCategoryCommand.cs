using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Categories.CreateCategory;

/// <summary>Command tạo danh mục mới.</summary>
public record CreateCategoryCommand(
    /// <summary>Tên danh mục.</summary>
    string Name,
    /// <summary>Mô tả danh mục (không bắt buộc).</summary>
    string? Description) : IRequest<Result<Guid>>;
