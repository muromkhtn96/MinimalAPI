using MediatR;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Application.Features.Categories.DeleteCategory;

/// <summary>Command xóa danh mục.</summary>
public record DeleteCategoryCommand(
    /// <summary>Mã danh mục cần xóa.</summary>
    Guid Id) : IRequest<Result<Guid>>;
