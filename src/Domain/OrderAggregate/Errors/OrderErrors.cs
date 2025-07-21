using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.Errors;

public static class OrderErrors
{
    public static readonly Error InvalidOrderId =
        Error.Validation("Order.InvalidId", "Invalid Order Id");

    public static readonly Error OrderItemRequired =
        Error.Validation("Order.OrderItemRequired", "An order must have at least one item.");

    public static readonly Error InvalidOrderStatusForAccept =
        Error.Validation("Order.InvalidOrderStatusForAccept", "Order cannot be accepted because it is not in 'Placed' status.");

    public static readonly Error InvalidOrderStatusForCancel =
        Error.Validation("Order.InvalidOrderStatusForCancel", "Order cannot be cancelled at its current stage.");

    public static readonly Error CouponCannotBeAppliedToOrderStatus =
        Error.Validation("Order.CouponCannotBeApplied", "A coupon cannot be applied to an order in its current status.");

    public static readonly Error CouponAlreadyApplied =
        Error.Validation("Order.CouponAlreadyApplied", "A coupon has already been applied to this order. Please remove it first.");

    public static readonly Error CouponNotApplicable =
        Error.Validation("Order.CouponNotApplicable", "This coupon is not applicable to the items in your order.");

    public static readonly Error NegativeTotalAmount =
        Error.Validation("Order.NegativeTotalAmount", "The total amount for an order cannot be negative.");

    public static readonly Error PaymentMismatch =
        Error.Validation("Order.PaymentMismatch", "Payment transaction total does not match order total amount.");

    public static readonly Error OrderItemInvalidQuantity =
        Error.Validation("OrderItem.InvalidQuantity", "Order item quantity must be positive.");

    public static readonly Error OrderItemInvalidName =
        Error.Validation("OrderItem.InvalidName", "Order item name cannot be empty.");

    public static readonly Error PaymentTransactionInvalidAmount =
        Error.Validation("PaymentTransaction.InvalidAmount", "Payment transaction amount must be positive.");

    public static readonly Error AddressInvalid =
        Error.Validation("Address.Invalid", "All address fields are required.");

    public static readonly Error OrderItemCustomizationInvalid =
        Error.Validation("OrderItemCustomization.Invalid", "Customization group and choice names cannot be empty.");

    public static readonly Error InvalidStatusForReject =
        Error.Validation("Order.InvalidStatusForReject", "Order cannot be rejected at its current stage.");

    public static readonly Error PaymentNotFound =
        Error.Validation("Order.PaymentNotFound", "The specified payment transaction was not found.");

    public static readonly Error InvalidOrderStatusForPreparing =
        Error.Validation("Order.InvalidOrderStatusForPreparing", "Order cannot be marked as preparing from its current status.");

    public static readonly Error InvalidOrderStatusForReadyForDelivery =
        Error.Validation("Order.InvalidOrderStatusForReadyForDelivery", "Order cannot be marked as ready for delivery from its current status.");

    public static readonly Error InvalidOrderStatusForDelivered =
        Error.Validation("Order.InvalidOrderStatusForDelivered", "Order cannot be marked as delivered from its current status.");
        
    public static readonly Error InvalidStatusForPaymentConfirmation =
        Error.Validation("Order.InvalidStatusForPaymentConfirmation", "Payment can only be confirmed for orders in 'PendingPayment' status.");
        
    public static readonly Error FinancialMismatch =
        Error.Validation("Order.FinancialMismatch", "The calculated total amount does not match the provided total amount.");
        
    public static readonly Error PaymentIntentIdRequired =
        Error.Validation("Order.PaymentIntentIdRequired", "Payment intent ID is required for orders in 'PendingPayment' status.");
}
