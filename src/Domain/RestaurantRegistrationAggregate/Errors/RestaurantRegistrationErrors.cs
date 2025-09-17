using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantRegistrationAggregate.Errors;

public static class RestaurantRegistrationErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "RestaurantRegistration.NotFound",
        $"Restaurant registration '{id}' was not found.");

    public static Error NotPending => Error.Conflict(
        "RestaurantRegistration.NotPending",
        "Registration is not in a pending state.");

    public static Error AlreadyApproved => Error.Conflict(
        "RestaurantRegistration.AlreadyApproved",
        "Registration has already been approved.");

    public static Error AlreadyRejected => Error.Conflict(
        "RestaurantRegistration.AlreadyRejected",
        "Registration has already been rejected.");

    public static Error InvalidField(string field, string reason) => Error.Validation(
        $"RestaurantRegistration.{field}",
        $"Invalid {field}: {reason}");

    public static Error ReasonIsRequired => Error.Validation(
        "RestaurantRegistration.ReasonRequired",
        "Rejection reason is required.");
}

