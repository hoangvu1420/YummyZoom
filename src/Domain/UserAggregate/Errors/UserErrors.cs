using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UserAggregate.Errors;

public static class UserErrors
{
    public static Error InvalidUserId(string value) => Error.Validation(
        "User.InvalidUserId",
        $"User ID '{value}' is not a valid GUID.");

    public static Error UserNotFound(Guid userId) => Error.NotFound(
        "User.UserNotFound",
        $"User with ID '{userId}' not found.");

    public static Error RegistrationFailed(string message) => Error.Failure(
        "User.RegistrationFailed",
        $"User registration failed: {message}");

    public static Error EmailUpdateFailed(string message) => Error.Failure(
        "User.EmailUpdateFailed",
        $"Email update failed: {message}");

    public static Error ProfileUpdateFailed(string message) => Error.Failure(
        "User.ProfileUpdateFailed",
        $"Profile update failed: {message}");

    public static Error DuplicateEmail(string email) => Error.Validation(
        "User.DuplicateEmail",
        $"Email '{email}' is already registered.");

    public static Error DeletionFailed(string message) => Error.Failure(
        "User.DeletionFailed",
        $"User deletion failed: {message}");

    public static Error AddressNotFound(Guid addressId) => Error.NotFound(
        "User.AddressNotFound",
        $"Address with ID '{addressId}' not found for the user.");

    public static Error PaymentMethodNotFound(Guid paymentMethodId) => Error.NotFound(
        "User.PaymentMethodNotFound",
        $"Payment method with ID '{paymentMethodId}' not found for the user.");

    public static Error InvalidPaymentMethod => Error.Validation(
        "User.InvalidPaymentMethod",
        "Payment method is invalid.");
}
