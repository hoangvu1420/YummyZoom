namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Service for managing per-phone OTP throttling and lockout state.
/// Provides atomic operations for tracking request counts, failed verification attempts,
/// and lockout periods to prevent abuse of OTP endpoints.
/// </summary>
public interface IOtpThrottleStore
{
    #region Request Throttling

    /// <summary>
    /// Atomically increments the request count for a phone number within the specified window.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="windowMinutes">The time window in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new count after increment</returns>
    Task<int> IncrementRequestCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current request count for a phone number within the specified window.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="windowMinutes">The time window in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current request count</returns>
    Task<int> GetRequestCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of seconds until the next request is allowed for the phone number.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="windowMinutes">The time window in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Seconds until next request is allowed, or 0 if allowed now</returns>
    Task<int> GetRetryAfterSecondsAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the request count for a phone number (typically called after successful verification).
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetRequestCountAsync(string phoneNumber, CancellationToken cancellationToken = default);

    #endregion

    #region Verification Failure Tracking & Lockout

    /// <summary>
    /// Records a failed verification attempt for a phone number.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new failed attempt count</returns>
    Task<int> RecordFailedVerifyAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current failed verification count for a phone number within the specified window.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="windowMinutes">The time window in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current failed verification count</returns>
    Task<int> GetFailedVerifyCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a lockout period for a phone number.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="lockoutMinutes">Duration of lockout in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetLockoutAsync(string phoneNumber, int lockoutMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining lockout time in seconds for a phone number.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Seconds remaining in lockout, or 0 if not locked out</returns>
    Task<int> GetLockoutRemainingSecondsAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a phone number is currently locked out.
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if locked out, false otherwise</returns>
    Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets failed verification counts and removes lockout for a phone number 
    /// (typically called after successful verification).
    /// </summary>
    /// <param name="phoneNumber">The normalized phone number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetFailedVerifyAndLockoutAsync(string phoneNumber, CancellationToken cancellationToken = default);

    #endregion
}
