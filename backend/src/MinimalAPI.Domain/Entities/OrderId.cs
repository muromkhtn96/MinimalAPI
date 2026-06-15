namespace MinimalAPI.Domain.Entities;
public readonly record struct OrderId(Guid Value)
{
    /// <summary> Tạo một OrderId mới với giá trị ngẫu nhiên. </summary>
    public static OrderId New() => new(Guid.NewGuid());
}