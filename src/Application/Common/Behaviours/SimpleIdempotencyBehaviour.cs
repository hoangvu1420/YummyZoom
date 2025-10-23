using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Idempotency;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Behaviours;

/// <summary>
/// Pipeline behavior that provides simple idempotency for commands implementing IIdempotentCommand.
/// Caches successful responses for a short period (5 minutes) to prevent duplicate execution.
/// </summary>
public sealed class SimpleIdempotencyBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cache;
    private readonly IUser _currentUser;
    private readonly ILogger<SimpleIdempotencyBehaviour<TRequest, TResponse>> _logger;

    // Cache successful responses for 5 minutes to prevent immediate duplicates
    private static readonly CachePolicy IdempotencyPolicy = CachePolicy.WithTtl(TimeSpan.FromMinutes(5), "idempotency");

    public SimpleIdempotencyBehaviour(
        ICacheService cache,
        IUser currentUser,
        ILogger<SimpleIdempotencyBehaviour<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only apply idempotency to commands that opt-in
        if (request is not IIdempotentCommand idempotentCommand)
        {
            return await next();
        }

        // Skip if no idempotency key provided
        if (string.IsNullOrWhiteSpace(idempotentCommand.IdempotencyKey))
        {
            return await next();
        }

        // Validate idempotency key format
        var keyResult = IdempotencyKey.Create(idempotentCommand.IdempotencyKey);
        if (keyResult.IsFailure)
        {
            _logger.LogWarning("Invalid idempotency key format: {Key}", idempotentCommand.IdempotencyKey);
            // For invalid keys, we could either fail or continue without idempotency
            // For MVP, let's continue without idempotency to be more forgiving
            return await next();
        }

        var cacheKey = GenerateCacheKey(keyResult.Value, request);

        try
        {
            // Check if we already have a cached response
            var cachedResponse = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
            if (cachedResponse is not null)
            {
                _logger.LogInformation("Idempotent request detected, returning cached response. Key: {IdempotencyKey}, Command: {CommandType}",
                    keyResult.Value, typeof(TRequest).Name);
                return cachedResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached response for idempotency key: {Key}", keyResult.Value);
            // Continue with execution if cache retrieval fails
        }

        // Execute the command
        var response = await next();

        // Cache only successful responses
        bool shouldCache = true;
        if (response is Result baseResult)
        {
            shouldCache = baseResult.IsSuccess;
        }

        if (shouldCache)
        {
            try
            {
                await _cache.SetAsync(cacheKey, response, IdempotencyPolicy, cancellationToken);
                _logger.LogDebug("Cached successful response for idempotency key: {Key}, Command: {CommandType}",
                    keyResult.Value, typeof(TRequest).Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache response for idempotency key: {Key}", keyResult.Value);
                // Don't fail the request if caching fails
            }
        }

        return response;
    }

    /// <summary>
    /// Generates a cache key that includes the user ID, command type, and idempotency key
    /// to ensure proper isolation between users and command types.
    /// </summary>
    private string GenerateCacheKey(IdempotencyKey idempotencyKey, TRequest request)
    {
        var userId = _currentUser.DomainUserId?.Value.ToString() ?? "anonymous";
        var commandType = typeof(TRequest).Name;
        
        return $"idem:{userId}:{commandType}:{idempotencyKey}";
    }
}
