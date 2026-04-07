using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MinimalAPI.Application.Behaviors;

/// <summary>
/// Pipeline behavior — log tên request và thời gian xử lý.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();

        logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms",
            requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
