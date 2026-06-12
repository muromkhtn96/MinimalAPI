namespace MinimalAPI.Domain.Entities;
public readonly record struct OrderDetailId(Guid Value)
{
    /// <summary> Tạo một OrderDetailId mới với giá trị ngẫu nhiên. </summary>
    public static OrderDetailId New() => new(Guid.NewGuid());
}