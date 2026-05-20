namespace MinimalAPI.Application.Abstractions;

/// <summary>
/// Helper sinh mã code tuần tự theo prefix (VD: KH00001, DH00001, SP00001).
/// Mỗi prefix có counter riêng, lưu trong bảng <c>code_counters</c>.
/// An toàn dưới concurrency nhờ atomic UPSERT của PostgreSQL.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Sinh mã code kế tiếp cho prefix.
    /// </summary>
    /// <param name="prefix">Tiền tố mã (VD: <c>"KH"</c>, <c>"DH"</c>, <c>"SP"</c>). Chỉ chấp nhận A-Z, độ dài 1–10.</param>
    /// <param name="padLength">Số chữ số phần số (mặc định 5 → <c>KH00001</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mã code dạng <c>{PREFIX}{NUMBER:PadLeft}</c>.</returns>
    Task<string> NextAsync(string prefix, int padLength = 5, CancellationToken ct = default);
}
