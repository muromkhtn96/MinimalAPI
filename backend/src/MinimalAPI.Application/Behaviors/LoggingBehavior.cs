using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MinimalAPI.Application.Behaviors;

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

        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();

        logger.LogInformation("{RequestName} hoàn thành trong {ElapsedMs}ms",
            requestName, sw.ElapsedMilliseconds);

        if (sw.ElapsedMilliseconds > 500)
            logger.LogWarning("{RequestName} mất {ElapsedMs}ms — chậm hơn mong đợi",
                requestName, sw.ElapsedMilliseconds);

        return response;
    }
}