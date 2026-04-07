namespace MinimalAPI.Domain.Entities;

/// <summary>
/// Strongly Typed ID cho Product — tránh nhầm lẫn Guid giữa các entity.
/// Dùng readonly record struct để lightweight (value type, không allocate heap).
/// </summary>
public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
}
