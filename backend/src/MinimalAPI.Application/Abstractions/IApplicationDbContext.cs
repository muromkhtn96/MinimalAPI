using MinimalAPI.Domain.Entities;

namespace MinimalAPI.Application.Abstractions;

/// <summary>
/// Interface cho DbContext — Application layer dùng để query (LINQ).
/// Giữ dependency rule: Application không reference Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    IQueryable<Product> Products { get; }
    IQueryable<Category> Categories { get; }
}
