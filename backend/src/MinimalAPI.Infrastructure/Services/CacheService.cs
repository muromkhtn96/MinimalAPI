using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using MinimalAPI.Application.Abstractions;

namespace MinimalAPI.Infrastructure.Services;

/// <summary>
/// Triển khai ICacheService sử dụng HybridCache của .NET 9
/// </summary>
public class CacheService(HybridCache hybridCache) : ICacheService
{
    public async Task<T?> GetOrCreateAsync<T>(
        string key, 
        Func<CancellationToken, Task<T?>> factory, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default)
    {
        var options = expiration.HasValue 
            ? new HybridCacheEntryOptions { Expiration = expiration.Value } 
            : null;

        return await hybridCache.GetOrCreateAsync<T?>(
            key, 
            async token => await factory(token), 
            options, 
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await hybridCache.RemoveAsync(key, cancellationToken: cancellationToken);
    }
}