using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands;

public static class UserDeviceErrors
{
    public static Error TokenNotFound(string fcmToken) => Error.NotFound(
        "UserDevice.TokenNotFound",
        $"FCM token '{fcmToken}' not found or is inactive.");

    public static Error DeviceRegistrationFailed(string message) => Error.Failure(
        "UserDevice.RegistrationFailed",
        $"Device registration failed: {message}");

    public static Error DeviceUnregistrationFailed(string message) => Error.Failure(
        "UserDevice.UnregistrationFailed",
        $"Device unregistration failed: {message}");

    public static Error TokenAlreadyRegistered(string fcmToken) => Error.Validation(
        "UserDevice.TokenAlreadyRegistered",
        $"FCM token '{fcmToken}' is already registered to another user.");

    public static Error InvalidDeviceData() => Error.Validation(
        "UserDevice.InvalidDeviceData",
        "Device ID, Platform, and FCM Token cannot be empty.");
}
