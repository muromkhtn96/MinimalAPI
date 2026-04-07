using MinimalAPI.Domain.Exceptions;

namespace MinimalAPI.Domain.ValueObjects;

/// <summary>
/// Value Object cho tên sản phẩm — đảm bảo tên luôn hợp lệ.
/// Private constructor + static Create() = không thể tạo tên rỗng/quá dài.
/// </summary>
public sealed record ProductName
{
    public const int MaxLength = 200;

    public string Value { get; }

    private ProductName(string value) => Value = value;

    public static ProductName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Tên sản phẩm không được để trống.");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new DomainException($"Tên sản phẩm không được quá {MaxLength} ký tự.");

        return new ProductName(trimmed);
    }

    public override string ToString() => Value;
}
