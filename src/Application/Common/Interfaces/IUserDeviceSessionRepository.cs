using YummyZoom.SharedKernel.Models; // Reference the entity from SharedKernel

namespace YummyZoom.Application.Common.Interfaces;

public interface IUserDeviceSessionRepository
{
    /// <summary>
    /// Adds a new user device session.
    /// </summary>
    /// <param name="session">The UserDeviceSession entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddSessionAsync(UserDeviceSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all active sessions for a given device.
    /// </summary>
    /// <param name="deviceId">The ID of the device.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateSessionsForDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active FCM tokens for a given user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active FCM tokens.</returns>
    Task<List<string>> GetActiveFcmTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an active session by FCM token.
    /// </summary>
    /// <param name="fcmToken">The FCM token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active UserDeviceSession if found, otherwise null.</returns>
    Task<UserDeviceSession?> GetActiveSessionByTokenAsync(string fcmToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active FCM tokens across all users and devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all active FCM tokens.</returns>
    Task<List<string>> GetAllActiveFcmTokensAsync(CancellationToken cancellationToken = default);
}
