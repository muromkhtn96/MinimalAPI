namespace MinimalAPI.Domain.Exceptions;

/// <summary>
/// Exception cho lỗi business logic trong Domain layer.
/// Khác với validation error — đây là lỗi vi phạm business rule.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
