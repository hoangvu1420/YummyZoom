using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Notifications;

public static class NotificationErrors
{
    public static Error UserNotAuthenticated() => Error.Failure(
        "Notification.UserNotAuthenticated",
        "User must be authenticated to send notifications.");

    public static Error NoActiveDevices(UserId userId) => Error.NotFound(
        "Notification.NoActiveDevices",
        $"No active devices found for user '{userId.Value}'.");

    public static Error NoActiveDevicesForBroadcast() => Error.NotFound(
        "Notification.NoActiveDevicesForBroadcast",
        "No active devices found for broadcast notification.");

    public static Error FcmSendFailed(string message) => Error.Failure(
        "Notification.FcmSendFailed",
        $"Failed to send FCM notification: {message}");

    public static Error InvalidNotificationData() => Error.Validation(
        "Notification.InvalidData",
        "Notification title and body cannot be empty.");

    public static Error MulticastSendFailed(int failedCount, int totalCount) => Error.Failure(
        "Notification.MulticastSendFailed",
        $"Failed to send notification to {failedCount} out of {totalCount} devices.");

    public static Error BroadcastFailed(string message) => Error.Failure(
        "Notification.BroadcastFailed",
        $"Failed to send broadcast notification: {message}");

    public static Error UnauthorizedOperation() => Error.Failure(
        "Notification.UnauthorizedOperation",
        "You are not authorized to perform this notification operation.");
}
