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

    public static readonly Error NegativeTotalAmount =
        Error.Validation("Order.NegativeTotalAmount", "The total amount for an order cannot be negative.");

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
}
