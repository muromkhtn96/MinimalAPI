namespace MinimalAPI.Infrastructure.Persistence;

/// <summary>
/// Bảng lưu counter cho mỗi prefix mã code.
/// Là infrastructure concern thuần — không nằm trong Domain.
/// </summary>
internal sealed class CodeCounter
{
    public string Prefix { get; set; } = default!;
    public long CurrentValue { get; set; }
}
