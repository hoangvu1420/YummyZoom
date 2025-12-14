using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.ReviewAggregate.Errors;

public static class ReviewErrors
{
    public static Error InvalidRating => Error.Validation(
        "Review.InvalidRating",
        "Rating must be between 1 and 5");

    public static Error InvalidOrderId => Error.Validation(
        "Review.InvalidOrderId",
        "Order ID cannot be null");

    public static Error InvalidCustomerId => Error.Validation(
        "Review.InvalidCustomerId",
        "Customer ID cannot be null");

    public static Error InvalidRestaurantId => Error.Validation(
        "Review.InvalidRestaurantId",
        "Restaurant ID cannot be null");

    public static Error EmptyReply => Error.Validation(
        "Review.EmptyReply",
        "Reply cannot be null or empty");

    public static Error ReviewAlreadyReplied => Error.Conflict(
        "Review.ReviewAlreadyReplied",
        "Review has already been replied to");

    public static Error NotFound => Error.NotFound(
        "Review.NotFound",
        "Review not found");
}
