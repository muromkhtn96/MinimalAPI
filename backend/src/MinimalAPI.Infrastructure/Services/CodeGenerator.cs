using Microsoft.EntityFrameworkCore;
using MinimalAPI.Application.Abstractions;
using MinimalAPI.Infrastructure.Persistence;
using Npgsql;

namespace MinimalAPI.Infrastructure.Services;

/// <summary>
/// Sinh mã code tuần tự theo prefix, an toàn dưới concurrency
/// nhờ atomic UPSERT (INSERT ... ON CONFLICT ... RETURNING) của PostgreSQL.
/// </summary>
public sealed class CodeGenerator(AppDbContext db) : ICodeGenerator
{
    private const int MaxPrefixLength = 10;
    private const int MinPadLength = 1;
    private const int MaxPadLength = 20;

    public async Task<string> NextAsync(string prefix, int padLength = 5, CancellationToken ct = default)
    {
        var normalizedPrefix = NormalizeAndValidatePrefix(prefix);

        if (padLength is < MinPadLength or > MaxPadLength)
            throw new ArgumentOutOfRangeException(nameof(padLength),
                $"padLength phải trong khoảng [{MinPadLength}, {MaxPadLength}].");

        // Atomic UPSERT — Postgres đảm bảo không race condition.
        // Lần đầu: INSERT current_value = 1.
        // Lần sau: UPDATE current_value = current_value + 1.
        // RETURNING trả về giá trị sau cùng — chính là số kế tiếp.
        // EF Core SqlQueryRaw<long> yêu cầu cột kết quả tên "Value"
        const string sql = """
            INSERT INTO code_counters (prefix, current_value)
            VALUES (@prefix, 1)
            ON CONFLICT (prefix)
            DO UPDATE SET current_value = code_counters.current_value + 1
            RETURNING current_value AS "Value";
            """;

        var prefixParam = new NpgsqlParameter("@prefix", normalizedPrefix);

        var nextList = await db.Database
            .SqlQueryRaw<long>(sql, prefixParam)
            .ToListAsync(ct);
        var next = nextList.First();

        return $"{normalizedPrefix}{next.ToString().PadLeft(padLength, '0')}";
    }

    private static string NormalizeAndValidatePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix không được rỗng.", nameof(prefix));

        var trimmed = prefix.Trim().ToUpperInvariant();

        if (trimmed.Length > MaxPrefixLength)
            throw new ArgumentException(
                $"Prefix tối đa {MaxPrefixLength} ký tự.", nameof(prefix));

        foreach (var c in trimmed)
        {
            if (c is < 'A' or > 'Z')
                throw new ArgumentException(
                    "Prefix chỉ được chứa ký tự A-Z.", nameof(prefix));
        }

        return trimmed;
    }
}
