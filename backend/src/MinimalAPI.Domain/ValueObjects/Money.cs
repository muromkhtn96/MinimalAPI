using MinimalAPI.Domain.Exceptions;

namespace MinimalAPI.Domain.ValueObjects;

/// <summary>
/// Value Object đại diện cho tiền tệ.
/// Value Object: so sánh bằng giá trị (Amount + Currency), không có Id.
/// Khác Entity: Entity so sánh bằng Id.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Tạo Money với validation.</summary>
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Số tiền không được âm.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Mã tiền tệ phải đúng 3 ký tự (VD: VND, USD).");

        return new Money(amount, currency.Trim().ToUpperInvariant());
    }

    /// <summary>Tạo Money VND nhanh.</summary>
    public static Money VND(decimal amount) => Create(amount, "VND");

    /// <summary>Giá trị 0 VND.</summary>
    public static Money Zero => new(0, "VND");

    /// <summary>Cộng tiền — phải cùng loại tiền tệ.</summary>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new DomainException($"Không thể cộng {left.Currency} với {right.Currency}.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>Nhân tiền với số lượng.</summary>
    public static Money operator *(Money money, int quantity)
    {
        if (quantity < 0)
            throw new DomainException("Số lượng không được âm.");

        return new Money(money.Amount * quantity, money.Currency);
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}
