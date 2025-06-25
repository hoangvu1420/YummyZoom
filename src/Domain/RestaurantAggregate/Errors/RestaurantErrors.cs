
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RestaurantAggregate.Errors;

public static class RestaurantErrors
{
    public static readonly Error InvalidRestaurantId = new(
        "Restaurant.InvalidRestaurantId",
        "Invalid restaurant ID.",
        ErrorType.Validation);

    public static readonly Error NameIsRequired = new(
        "Restaurant.NameIsRequired",
        "Restaurant name is required.",
        ErrorType.Validation);

    public static readonly Error LocationIsRequired = new(
        "Restaurant.LocationIsRequired",
        "Restaurant location is required.",
        ErrorType.Validation);
}
