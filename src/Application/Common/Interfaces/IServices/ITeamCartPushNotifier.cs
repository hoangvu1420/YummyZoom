using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Sends FCM pushes (hybrid or data-only) for TeamCart changes to targeted member devices.
/// Hybrid payload: notification { title, body, sound }, data { type, teamCartId, version, state, click_action, route, actorId, event }
/// Data-only payload: data { type, teamCartId, version, state, click_action, route, actorId, event }
/// </summary>
public interface ITeamCartPushNotifier
{
    /// <summary>
    /// Sends push notification with targeting and contextual information.
    /// </summary>
    /// <param name="teamCartId">The TeamCart ID</param>
    /// <param name="version">The version of the TeamCart</param>
    /// <param name="target">Who should receive the notification</param>
    /// <param name="context">Contextual information for generating event-specific messages</param>
    /// <param name="deliveryType">Whether to send hybrid (notification tray) or data-only (silent) notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result> PushTeamCartDataAsync(
        TeamCartId teamCartId, 
        long version,
        TeamCartNotificationTarget target = TeamCartNotificationTarget.All,
        TeamCartNotificationContext? context = null,
        NotificationDeliveryType deliveryType = NotificationDeliveryType.Hybrid,
        CancellationToken cancellationToken = default);
}

