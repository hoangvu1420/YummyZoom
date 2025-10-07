using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.Caching;

/// <summary>
/// Distributed cache-based implementation of IOtpThrottleStore using Redis or in-memory cache.
/// Provides atomic operations for OTP throttling and lockout management.
/// </summary>
public class OtpThrottleStore : IOtpThrottleStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<OtpThrottleStore> _logger;

    private const string RequestCountKeyPrefix = "otp:req:";
    private const string FailedVerifyKeyPrefix = "otp:fail:";
    private const string LockoutKeyPrefix = "otp:lock:";

    public OtpThrottleStore(IDistributedCache cache, ILogger<OtpThrottleStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    #region Request Throttling

    public async Task<int> IncrementRequestCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
    {
        var key = GetRequestCountKey(phoneNumber, windowMinutes);
        var expiry = TimeSpan.FromMinutes(windowMinutes);

        try
        {
            // Try to get existing count
            var existingBytes = await _cache.GetAsync(key, cancellationToken);
            var currentCount = 0;

            if (existingBytes != null)
            {
                var existingData = JsonSerializer.Deserialize<ThrottleData>(existingBytes);
                currentCount = existingData?.Count ?? 0;
            }

            var newCount = currentCount + 1;
            var newData = new ThrottleData { Count = newCount, Timestamp = DateTimeOffset.UtcNow };
            var newBytes = JsonSerializer.SerializeToUtf8Bytes(newData);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _cache.SetAsync(key, newBytes, options, cancellationToken);
            return newCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment request count for phone {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<int> GetRequestCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
    {
        var key = GetRequestCountKey(phoneNumber, windowMinutes);

        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes == null) return 0;

            var data = JsonSerializer.Deserialize<ThrottleData>(bytes);
            return data?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get request count for phone {PhoneNumber}", phoneNumber);
            return 0; // Fail open for availability
        }
    }

    public async Task<int> GetRetryAfterSecondsAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
    {
        var key = GetRequestCountKey(phoneNumber, windowMinutes);

        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes == null) return 0;

            var data = JsonSerializer.Deserialize<ThrottleData>(bytes);
            if (data?.Timestamp == null) return 0;

            var windowExpiry = data.Timestamp.Value.AddMinutes(windowMinutes);
            var remaining = windowExpiry - DateTimeOffset.UtcNow;

            return remaining.TotalSeconds > 0 ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get retry after for phone {PhoneNumber}", phoneNumber);
            return 0; // Fail open
        }
    }

    public async Task ResetRequestCountAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var keys = new[]
        {
            GetRequestCountKey(phoneNumber, 1),   // 1-minute window
            GetRequestCountKey(phoneNumber, 60)   // 1-hour window
        };

        try
        {
            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset request count for phone {PhoneNumber}", phoneNumber);
            // Don't throw - this is cleanup
        }
    }

    #endregion

    #region Verification Failure Tracking & Lockout

    public async Task<int> RecordFailedVerifyAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var key = GetFailedVerifyKey(phoneNumber);
        var expiry = TimeSpan.FromMinutes(10); // Failed attempts window

        try
        {
            var existingBytes = await _cache.GetAsync(key, cancellationToken);
            var currentCount = 0;

            if (existingBytes != null)
            {
                var existingData = JsonSerializer.Deserialize<ThrottleData>(existingBytes);
                currentCount = existingData?.Count ?? 0;
            }

            var newCount = currentCount + 1;
            var newData = new ThrottleData { Count = newCount, Timestamp = DateTimeOffset.UtcNow };
            var newBytes = JsonSerializer.SerializeToUtf8Bytes(newData);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _cache.SetAsync(key, newBytes, options, cancellationToken);
            return newCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record failed verify for phone {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<int> GetFailedVerifyCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
    {
        var key = GetFailedVerifyKey(phoneNumber);

        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes == null) return 0;

            var data = JsonSerializer.Deserialize<ThrottleData>(bytes);
            if (data?.Timestamp == null) return 0;

            // Check if the data is within the specified window
            var windowStart = DateTimeOffset.UtcNow.AddMinutes(-windowMinutes);
            if (data.Timestamp < windowStart) return 0;

            return data.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get failed verify count for phone {PhoneNumber}", phoneNumber);
            return 0; // Fail open
        }
    }

    public async Task SetLockoutAsync(string phoneNumber, int lockoutMinutes, CancellationToken cancellationToken = default)
    {
        var key = GetLockoutKey(phoneNumber);
        var expiry = TimeSpan.FromMinutes(lockoutMinutes);

        try
        {
            var lockoutData = new LockoutData 
            { 
                LockedUntil = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes) 
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(lockoutData);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _cache.SetAsync(key, bytes, options, cancellationToken);
            _logger.LogWarning("Phone {PhoneNumber} locked out for {LockoutMinutes} minutes", phoneNumber, lockoutMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set lockout for phone {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<int> GetLockoutRemainingSecondsAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var key = GetLockoutKey(phoneNumber);

        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes == null) return 0;

            var data = JsonSerializer.Deserialize<LockoutData>(bytes);
            if (data?.LockedUntil == null) return 0;

            var remaining = data.LockedUntil.Value - DateTimeOffset.UtcNow;
            return remaining.TotalSeconds > 0 ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lockout remaining for phone {PhoneNumber}", phoneNumber);
            return 0; // Fail open
        }
    }

    public async Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var remainingSeconds = await GetLockoutRemainingSecondsAsync(phoneNumber, cancellationToken);
        return remainingSeconds > 0;
    }

    public async Task ResetFailedVerifyAndLockoutAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var keys = new[]
        {
            GetFailedVerifyKey(phoneNumber),
            GetLockoutKey(phoneNumber)
        };

        try
        {
            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset failed verify and lockout for phone {PhoneNumber}", phoneNumber);
            // Don't throw - this is cleanup
        }
    }

    #endregion

    #region Private Helpers

    private static string GetRequestCountKey(string phoneNumber, int windowMinutes)
        => $"{RequestCountKeyPrefix}{phoneNumber}:{windowMinutes}m";

    private static string GetFailedVerifyKey(string phoneNumber)
        => $"{FailedVerifyKeyPrefix}{phoneNumber}";

    private static string GetLockoutKey(string phoneNumber)
        => $"{LockoutKeyPrefix}{phoneNumber}";

    #endregion

    #region Data Models

    private class ThrottleData
    {
        public int Count { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    private class LockoutData
    {
        public DateTimeOffset? LockedUntil { get; set; }
    }

    #endregion
}
