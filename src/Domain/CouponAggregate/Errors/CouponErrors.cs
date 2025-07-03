using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.CouponAggregate.Errors;

public static class CouponErrors
{
    public static Error InvalidCouponId(string value) => Error.Validation(
        "Coupon.InvalidCouponId",
        $"Coupon ID '{value}' is not a valid GUID.");

    public static Error CouponNotFound(Guid couponId) => Error.NotFound(
        "Coupon.CouponNotFound",
        $"Coupon with ID '{couponId}' not found.");

    public static Error CouponNotFound(string code, Guid restaurantId) => Error.NotFound(
        "Coupon.CouponNotFound",
        $"Coupon with code '{code}' not found for restaurant '{restaurantId}'.");

    public static Error CouponCodeEmpty => Error.Validation(
        "Coupon.CodeEmpty",
        "Coupon code cannot be null or empty.");

    public static Error CouponCodeTooLong(int maxLength) => Error.Validation(
        "Coupon.CodeTooLong",
        $"Coupon code cannot exceed {maxLength} characters.");

    public static Error CouponDescriptionEmpty => Error.Validation(
        "Coupon.DescriptionEmpty",
        "Coupon description cannot be null or empty.");

    public static Error InvalidValidityPeriod => Error.Validation(
        "Coupon.InvalidValidityPeriod",
        "Validity end date must be after start date.");

    public static Error InvalidUsageLimit => Error.Validation(
        "Coupon.InvalidUsageLimit",
        "Total usage limit must be greater than 0 when specified.");

    public static Error InvalidPerUserLimit => Error.Validation(
        "Coupon.InvalidPerUserLimit",
        "Per-user usage limit must be greater than 0 when specified.");

    public static Error CouponExpired => Error.Validation(
        "Coupon.Expired",
        "Coupon has expired and cannot be used.");

    public static Error CouponNotYetValid => Error.Validation(
        "Coupon.NotYetValid",
        "Coupon is not yet valid for use.");

    public static Error CouponDisabled => Error.Validation(
        "Coupon.Disabled",
        "Coupon is currently disabled and cannot be used.");

    public static Error UsageLimitExceeded => Error.Validation(
        "Coupon.UsageLimitExceeded",
        "Coupon usage limit has been exceeded.");

    public static Error UsageCountCannotExceedLimit(int currentCount, int? totalLimit) => Error.Validation(
        "Coupon.UsageCountCannotExceedLimit",
        $"Current usage count ({currentCount}) cannot exceed total limit ({totalLimit}).");

    public static Error InvalidMinOrderAmount => Error.Validation(
        "Coupon.InvalidMinOrderAmount",
        "Minimum order amount must be greater than 0 when specified.");

    public static Error CouponCodeAlreadyExists(string code, Guid restaurantId) => Error.Validation(
        "Coupon.CodeAlreadyExists",
        $"Coupon code '{code}' already exists for restaurant '{restaurantId}'.");

    public static Error CannotIncrementUsageWhenDisabled => Error.Validation(
        "Coupon.CannotIncrementUsageWhenDisabled",
        "Cannot increment usage count when coupon is disabled.");

    public static Error CannotIncrementUsageWhenExpired => Error.Validation(
        "Coupon.CannotIncrementUsageWhenExpired",
        "Cannot increment usage count when coupon is expired.");
}
