using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Service for sending Firebase Cloud Messaging (FCM) notifications
/// </summary>
public interface IFcmService
{
    /// <summary>
    /// Sends a notification message to a single device token
    /// </summary>
    /// <param name="fcmToken">The FCM token of the target device</param>
    /// <param name="title">The notification title</param>
    /// <param name="body">The notification body</param>
    /// <param name="data">Optional additional data to include with the notification</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends a data-only message to a single device token
    /// </summary>
    /// <param name="fcmToken">The FCM token of the target device</param>
    /// <param name="data">Data payload to send</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendDataMessageAsync(string fcmToken, Dictionary<string, string> data);

    /// <summary>
    /// Sends a notification message to multiple device tokens (multicast)
    /// </summary>
    /// <param name="fcmTokens">Collection of FCM tokens to send to</param>
    /// <param name="title">The notification title</param>
    /// <param name="body">The notification body</param>
    /// <param name="data">Optional additional data to include with the notification</param>
    /// <returns>Result with list of failed tokens on partial failure, or empty list on success</returns>
    Task<Result<List<string>>> SendMulticastNotificationAsync(IEnumerable<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Sends a data-only message to multiple device tokens (multicast)
    /// </summary>
    /// <param name="fcmTokens">Collection of FCM tokens to send to</param>
    /// <param name="data">Data payload to send</param>
    /// <returns>Result with list of failed tokens on partial failure, or empty list on success</returns>
    Task<Result<List<string>>> SendMulticastDataAsync(IEnumerable<string> fcmTokens, Dictionary<string, string> data);
}
