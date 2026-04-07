using MinimalAPI.Domain.Primitives;

namespace MinimalAPI.Domain.Entities;

/// <summary>
/// Aggregate Root — Danh mục sản phẩm.
/// </summary>
public sealed class Category : AggregateRoot<CategoryId>
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core cần parameterless constructor
    private Category() { }

    public static Category Create(string name, string? description)
    {
        var category = new Category
        {
            Id = CategoryId.New(),
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return category;
    }

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description?.Trim();
    }
}
