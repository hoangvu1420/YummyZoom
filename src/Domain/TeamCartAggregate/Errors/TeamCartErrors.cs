using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Errors;

/// <summary>
/// Contains all error definitions for the TeamCart aggregate.
/// </summary>
public static class TeamCartErrors
{
    // TeamCart errors
    public static readonly Error OnlyHostCanInitiateCheckout = Error.Validation(
        "TeamCart.OnlyHostCanInitiateCheckout",
        "Only the host can initiate checkout");

    public static readonly Error InvalidTransactionId = Error.Validation(
        "TeamCart.InvalidTransactionId",
        "Transaction ID cannot be empty");

    public static readonly Error InvalidTip = Error.Validation(
        "TeamCart.InvalidTip",
        "Tip amount cannot be negative.");

    // MemberPayment errors
    public static readonly Error MemberPaymentInvalidTransactionId = Error.Validation(
        "MemberPayment.InvalidTransactionId",
        "Transaction ID cannot be empty");

    public static readonly Error NotOnlinePayment = Error.Validation(
        "MemberPayment.NotOnlinePayment",
        "Cannot mark non-online payment as paid online");

    // TeamCartMember errors
    public static readonly Error UserIdRequired = Error.Validation(
        "TeamCartMember.UserIdRequired",
        "User ID is required");

    public static readonly Error NameRequired = Error.Validation(
        "TeamCartMember.NameRequired",
        "Member name is required");

    // TeamCartItem errors
    public static readonly Error ItemUserIdRequired = Error.Validation(
        "TeamCartItem.UserIdRequired",
        "User ID is required");

    public static readonly Error ItemNameRequired = Error.Validation(
        "TeamCartItem.ItemNameRequired",
        "Item name is required");

    // ID format errors
    public static readonly Error TeamCartIdEmpty = Error.Validation(
        "TeamCartId.InvalidFormat",
        "Team cart ID cannot be empty");

    public static readonly Error TeamCartIdInvalidFormat = Error.Validation(
        "TeamCartId.InvalidFormat",
        "Team cart ID must be a valid GUID");

    public static readonly Error TeamCartMemberIdEmpty = Error.Validation(
        "TeamCartMemberId.InvalidFormat",
        "Team cart member ID cannot be empty");

    public static readonly Error TeamCartMemberIdInvalidFormat = Error.Validation(
        "TeamCartMemberId.InvalidFormat",
        "Team cart member ID must be a valid GUID");

    public static readonly Error TeamCartItemIdInvalidFormat = Error.Validation(
        "TeamCartItemId.InvalidFormat",
        "Team cart item ID must be a valid GUID");

    public static readonly Error MemberPaymentIdEmpty = Error.Validation(
        "MemberPaymentId.Empty",
        "Member payment ID cannot be empty");

    public static readonly Error MemberPaymentIdInvalidFormat = Error.Validation(
        "MemberPaymentId.InvalidFormat",
        "Member payment ID format is invalid");

    public static readonly Error TeamCartNotFound = Error.NotFound(
        "TeamCart.NotFound",
        "Team cart not found");

    public static readonly Error InvalidTokenForJoining = Error.Validation(
        "TeamCart.InvalidToken",
        "Invalid token for joining the team cart");

    public static readonly Error TokenExpired = Error.Validation(
        "TeamCart.TokenExpired",
        "The token for joining the team cart has expired");

    public static readonly Error CannotAddItemsToClosedCart = Error.Validation(
        "TeamCart.ClosedCart",
        "Cannot add items to a closed team cart");

    public static readonly Error CannotAddMembersToClosedCart = Error.Validation(
        "TeamCart.ClosedCart",
        "Cannot add members to a closed team cart");

    public static readonly Error CannotJoinClosedCart = Error.Validation(
        "TeamCart.ClosedCart",
        "Cannot join a closed team cart");

    public static readonly Error CannotModifyClosedCart = Error.Validation(
        "TeamCart.ClosedCart",
        "Cannot modify a closed team cart");

    public static readonly Error PaymentNotCompleted = Error.Validation(
        "TeamCart.PaymentIncomplete",
        "Payment has not been completed");

    public static readonly Error InvalidStatusForConversion = Error.Validation(
        "TeamCart.InvalidStatus",
        "Team cart is not in a valid status for conversion to an order");

    public static readonly Error TeamCartExpired = Error.Validation(
        "TeamCart.Expired",
        "Team cart has expired");

    public static readonly Error HostCannotLeave = Error.Validation(
        "TeamCart.HostCannotLeave",
        "The host cannot leave the team cart");

    public static readonly Error MemberAlreadyExists = Error.Validation(
        "TeamCart.MemberExists",
        "Member already exists in the team cart");

    public static readonly Error DeadlineInPast = Error.Validation(
        "TeamCart.DeadlineInPast",
        "Deadline cannot be in the past");

    public static readonly Error HostNameRequired = Error.Validation(
        "TeamCart.HostNameRequired",
        "Host name is required");

    public static readonly Error MemberNameRequired = Error.Validation(
        "TeamCart.MemberNameRequired",
        "Member name is required");

    public static readonly Error OnlyHostCanSetDeadline = Error.Validation(
        "TeamCart.OnlyHostCanSetDeadline",
        "Only the host can set the deadline");

    // Item management errors
    public static readonly Error InvalidCustomization = Error.Validation(
        "TeamCart.InvalidCustomization",
        "Invalid customization data provided");

    public static readonly Error InvalidQuantity = Error.Validation(
        "TeamCart.InvalidQuantity",
        "Quantity must be greater than zero");

    public static readonly Error UserNotMember = Error.Validation(
        "TeamCart.UserNotMember",
        "User is not a member of this team cart");

    public static readonly Error MenuItemRequired = Error.Validation(
        "TeamCart.MenuItemRequired",
        "Menu item information is required");

    // Payment workflow errors
    public static readonly Error PaymentAlreadyCommitted = Error.Validation(
        "TeamCart.PaymentAlreadyCommitted",
        "Member has already committed to a payment method");

    public static readonly Error InvalidPaymentAmount = Error.Validation(
        "TeamCart.InvalidPaymentAmount",
        "Payment amount is invalid or does not match member's total");

    public static readonly Error PaymentNotFound = Error.Validation(
        "TeamCart.PaymentNotFound",
        "Payment record not found for the specified member");

    public static readonly Error CannotCommitPaymentInCurrentStatus = Error.Validation(
        "TeamCart.CannotCommitPaymentInCurrentStatus",
        "Cannot commit to payment in the current team cart status");

    public static readonly Error OnlinePaymentRequired = Error.Validation(
        "TeamCart.OnlinePaymentRequired",
        "Online payment is required but not completed");

    public static readonly Error InvalidPaymentMethod = Error.Validation(
        "TeamCart.InvalidPaymentMethod",
        "Invalid payment method specified");

    public static readonly Error CannotInitiateCheckoutWithoutItems = Error.Validation(
        "TeamCart.CannotInitiateCheckoutWithoutItems",
        "Cannot initiate checkout without any items in the cart");

    public static readonly Error CannotInitiateCheckoutWithoutMembers = Error.Validation(
        "TeamCart.CannotInitiateCheckoutWithoutMembers",
        "Cannot initiate checkout without any members in the cart");

    public static readonly Error ConversionDataIncomplete = Error.Validation(
        "TeamCart.ConversionDataIncomplete",
        "Incomplete data for converting team cart to order");

    public static readonly Error PaymentMismatchDuringConversion = Error.Validation(
        "TeamCart.PaymentMismatchDuringConversion",
        "Payment totals do not match during order conversion");

    public static readonly Error CannotConvertWithoutPayments = Error.Validation(
        "TeamCart.CannotConvertWithoutPayments",
        "Cannot convert team cart to order without payment commitments");

    // Financial management errors
    public static readonly Error OnlyHostCanModifyFinancials = Error.Validation(
        "TeamCart.OnlyHostCanModifyFinancials",
        "Only the host can modify financial details");

    public static readonly Error CannotModifyFinancialsInCurrentStatus = Error.Validation(
        "TeamCart.CannotModifyFinancialsInCurrentStatus",
        "Cannot modify financial details in the current team cart status");

    public static readonly Error CouponAlreadyApplied = Error.Validation(
        "TeamCart.CouponAlreadyApplied",
        "A coupon has already been applied to this team cart");

    public static readonly Error CouponNotApplicable = Error.Validation(
        "TeamCart.CouponNotApplicable",
        "This coupon is not applicable to the items in the team cart");

    // New errors for the Lock, Settle, Convert lifecycle
    public static readonly Error OnlyHostCanLockCart = Error.Validation(
        "TeamCart.OnlyHostCanLockCart",
        "Only the host can lock the cart for payment");

    public static readonly Error CannotLockCartInCurrentStatus = Error.Validation(
        "TeamCart.CannotLockCartInCurrentStatus",
        "Cannot lock the cart in its current status");

    public static readonly Error CannotLockEmptyCart = Error.Validation(
        "TeamCart.CannotLockEmptyCart",
        "Cannot lock an empty cart");

    public static readonly Error CannotModifyCartOnceLocked = Error.Validation(
        "TeamCart.CannotModifyCartOnceLocked",
        "Cannot modify items or members once the cart is locked");

    public static readonly Error CannotFinalizePricingInCurrentStatus = Error.Validation(
        "TeamCart.CannotFinalizePricingInCurrentStatus",
        "Can only finalize pricing when cart is in Locked status");

    public static readonly Error CanOnlyApplyFinancialsToLockedCart = Error.Validation(
        "TeamCart.CanOnlyApplyFinancialsToLockedCart",
        "Financial adjustments (tip/coupon) can only be applied before pricing is finalized");

    public static readonly Error CanOnlyPayOnFinalizedCart = Error.Validation(
        "TeamCart.CanOnlyPayOnFinalizedCart",
        "Payments can only be made after pricing is finalized");

    public static readonly Error FinalPaymentMismatch = Error.Validation(
        "TeamCart.FinalPaymentMismatch",
        "The sum of payment transactions does not match the total order amount");

    public static Error QuoteVersionMismatch(long currentVersion) => Error.Conflict(
        "TeamCart.QuoteVersionMismatch",
        $"Quote version mismatch. Current version is {currentVersion}. Please refresh and try again.");
}
