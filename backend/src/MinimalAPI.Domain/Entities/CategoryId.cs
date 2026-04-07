namespace MinimalAPI.Domain.Entities;

/// <summary>
/// Strongly Typed ID cho Category.
/// </summary>
public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New() => new(Guid.NewGuid());
}
