using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Caching;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Behaviours;

public sealed class CachingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehaviour<TRequest, TResponse>> _logger;

    public CachingBehaviour(ICacheService cache, ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery<TResponse> cacheable)
        {
            return await next();
        }

        try
        {
            var cached = await _cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheable.CacheKey);
                return cached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache get failed for {CacheKey}", cacheable.CacheKey);
        }

        var response = await next();

        // Cache only successful results to simplify invalidation semantics
        bool shouldCache = true;
        if (response is Result baseResult)
        {
            shouldCache = baseResult.IsSuccess;
        }

        if (shouldCache)
        {
            try
            {
                await _cache.SetAsync(cacheable.CacheKey, response!, cacheable.Policy, cancellationToken);
                _logger.LogDebug("Cache set for {CacheKey}", cacheable.CacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache set failed for {CacheKey}", cacheable.CacheKey);
            }
        }

        return response;
    }
}
