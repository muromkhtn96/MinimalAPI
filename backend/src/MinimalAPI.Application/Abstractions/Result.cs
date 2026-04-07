namespace MinimalAPI.Application.Abstractions;

/// <summary>
/// Result pattern — trả kết quả thành công hoặc lỗi, KHÔNG throw exception cho business logic.
/// </summary>
public record Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}
