using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Interfaces;

public interface IUserDeviceRepository
{
    /// <summary>
    /// Adds a new device or updates an existing device token for a user.
    /// If the token already exists, it updates the metadata (platform, last used date).
    /// </summary>
    Task AddOrUpdateAsync(UserId userId, string fcmToken, string platform, string? deviceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active FCM tokens for a given user.
    /// </summary>
    Task<List<string>> GetActiveFcmTokensByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active FCM tokens from all users (for broadcast notifications).
    /// </summary>
    Task<List<string>> GetAllActiveFcmTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a specific token as invalid/inactive (e.g., if FCM reports it's expired).
    /// </summary>
    Task MarkTokenAsInvalidAsync(string fcmToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly removes a token (e.g., on user logout from a specific device).
    /// </summary>
    Task RemoveTokenAsync(string fcmToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all tokens for a specific user (e.g., when user account is deleted).
    /// </summary>
    Task RemoveAllTokensForUserAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific FCM token exists and is active.
    /// </summary>
    Task<bool> TokenExistsAsync(string fcmToken, CancellationToken cancellationToken = default);
} 
