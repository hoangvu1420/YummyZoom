using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate;

/// <summary>
/// Represents an order placed by a customer within the YummyZoom system.
/// This is an Aggregate Root, ensuring consistency within the Order context.
/// Orders are immutable after creation - they can only be created and their status tracked.
/// </summary>
public sealed class Order : AggregateRoot<OrderId, Guid>, ICreationAuditable
{
    #region Fields

    private readonly List<OrderItem> _orderItems = [];
    private readonly List<PaymentTransaction> _paymentTransactions = [];

    #endregion

    #region Properties

    // Properties from ICreationAuditable
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets the unique identifier for this order.
    /// </summary>
    public new OrderId Id { get; private set; }
    
    /// <summary>
    /// Gets the human-readable order number displayed to users.
    /// </summary>
    public string OrderNumber { get; private set; }
    
    /// <summary>
    /// Gets the current status of the order in its lifecycle.
    /// </summary>
    public OrderStatus Status { get; private set; }
    
    /// <summary>
    /// Gets the timestamp when the order was initially placed.
    /// </summary>
    public DateTime PlacementTimestamp { get; private set; }
    
    /// <summary>
    /// Gets the timestamp when the order was last updated.
    /// </summary>
    public DateTime LastUpdateTimestamp { get; private set; }
    
    /// <summary>
    /// Gets the estimated delivery time provided by the restaurant.
    /// </summary>
    public DateTime? EstimatedDeliveryTime { get; private set; }
    
    /// <summary>
    /// Gets the actual time when the order was delivered to the customer.
    /// </summary>
    public DateTime? ActualDeliveryTime { get; private set; }
    
    /// <summary>
    /// Gets any special instructions provided by the customer for this order.
    /// </summary>
    public string SpecialInstructions { get; private set; }
    
    /// <summary>
    /// Gets the delivery address where the order should be delivered.
    /// </summary>
    public DeliveryAddress DeliveryAddress { get; private set; }
    
    /// <summary>
    /// Gets the subtotal amount of the order before discounts, fees, and taxes.
    /// </summary>
    public Money Subtotal { get; private set; }
    
    /// <summary>
    /// Gets the discount amount applied to the order.
    /// </summary>
    public Money DiscountAmount { get; private set; }
    
    /// <summary>
    /// Gets the delivery fee charged for the order.
    /// </summary>
    public Money DeliveryFee { get; private set; }
    
    /// <summary>
    /// Gets the tip amount added by the customer.
    /// </summary>
    public Money TipAmount { get; private set; }
    
    /// <summary>
    /// Gets the tax amount calculated for the order.
    /// </summary>
    public Money TaxAmount { get; private set; }
    
    /// <summary>
    /// Gets the total amount charged for the order.
    /// </summary>
    public Money TotalAmount { get; private set; }
    
    /// <summary>
    /// Gets the ID of the customer who placed the order.
    /// </summary>
    public UserId CustomerId { get; private set; }
    
    /// <summary>
    /// Gets the ID of the restaurant fulfilling the order.
    /// </summary>
    public RestaurantId RestaurantId { get; private set; }
    
    /// <summary>
    /// Gets the ID of the team cart that was converted to this order, if applicable.
    /// </summary>
    public TeamCartId? SourceTeamCartId { get; private set; }
    
    /// <summary>
    /// Gets the ID of the coupon applied to this order, if applicable.
    /// </summary>
    public CouponId? AppliedCouponId { get; private set; }

    /// <summary>
    /// Gets the payment intent ID for online payments. This is used to link webhook events back to the order.
    /// </summary>
    public string? PaymentIntentId { get; private set; }

    /// <summary>
    /// Gets a read-only list of all items in this order.
    /// </summary>
    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();
    
    /// <summary>
    /// Gets a read-only list of all payment transactions associated with this order.
    /// </summary>
    public IReadOnlyList<PaymentTransaction> PaymentTransactions => _paymentTransactions.AsReadOnly();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Order"/> class.
    /// Private constructor enforced by DDD for controlled creation via static factory method.
    /// </summary>
    private Order(
        OrderId orderId,
        string orderNumber,
        UserId customerId,
        RestaurantId restaurantId,
        DeliveryAddress deliveryAddress,
        List<OrderItem> orderItems,
        string specialInstructions,
        Money subtotal,
        Money discountAmount,
        Money deliveryFee,
        Money tipAmount,
        Money taxAmount,
        Money totalAmount,
        List<PaymentTransaction> paymentTransactions,
        CouponId? appliedCouponId,
        TeamCartId? sourceTeamCartId,
        OrderStatus initialStatus,
        string? paymentIntentId,
        DateTime timestamp)
        : base(orderId)
    {
        Id = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        RestaurantId = restaurantId;
        DeliveryAddress = deliveryAddress;
        SpecialInstructions = specialInstructions;
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        DeliveryFee = deliveryFee;
        TipAmount = tipAmount;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        AppliedCouponId = appliedCouponId;
        SourceTeamCartId = sourceTeamCartId;
        PaymentIntentId = paymentIntentId;

        _paymentTransactions = new List<PaymentTransaction>(paymentTransactions);
        _orderItems = new List<OrderItem>(orderItems);

        Status = initialStatus;
        PlacementTimestamp = timestamp;
        LastUpdateTimestamp = timestamp;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a new order instance after all business logic and calculations have been performed.
    /// This factory acts as a final consistency gatekeeper.
    /// </summary>
    public static Result<Order> Create(
        UserId customerId,
        RestaurantId restaurantId,
        DeliveryAddress deliveryAddress,
        List<OrderItem> orderItems,
        string specialInstructions,
        Money subtotal,
        Money discountAmount,
        Money deliveryFee,
        Money tipAmount,
        Money taxAmount,
        Money totalAmount, 
        List<PaymentTransaction> paymentTransactions,
        CouponId? appliedCouponId,
        TeamCartId? sourceTeamCartId = null)
    {
        // Default to Placed status for backward compatibility (COD orders)
        return Create(
            customerId,
            restaurantId,
            deliveryAddress,
            orderItems,
            specialInstructions,
            subtotal,
            discountAmount,
            deliveryFee,
            tipAmount,
            taxAmount,
            totalAmount,
            paymentTransactions,
            appliedCouponId,
            OrderStatus.Placed,
            null,
            sourceTeamCartId);
    }

    /// <summary>
    /// Creates a new order instance with support for the two-phase payment flow.
    /// </summary>
    public static Result<Order> Create(
        UserId customerId,
        RestaurantId restaurantId,
        DeliveryAddress deliveryAddress,
        List<OrderItem> orderItems,
        string specialInstructions,
        Money subtotal,
        Money discountAmount,
        Money deliveryFee,
        Money tipAmount,
        Money taxAmount,
        Money totalAmount, 
        List<PaymentTransaction>? paymentTransactions,
        CouponId? appliedCouponId,
        OrderStatus initialStatus,
        string? paymentIntentId = null,
        TeamCartId? sourceTeamCartId = null,
        DateTime? timestamp = null)
    {
        // Initialize paymentTransactions to empty list if null
        paymentTransactions ??= new List<PaymentTransaction>();
        
        if (!orderItems.Any())
        {
            return Result.Failure<Order>(OrderErrors.OrderItemRequired);
        }

        // Invariant Check 1 (Financial Integrity):
        // Asserts that the internally calculated total equals the `totalAmount` parameter.
        var calculatedTotal = subtotal - discountAmount + deliveryFee + tipAmount + taxAmount;
        if (calculatedTotal.Amount != totalAmount.Amount)
        {
            return Result.Failure<Order>(OrderErrors.FinancialMismatch);
        }
        
        // Ensure total is not negative
        if (totalAmount.Amount < 0)
        {
            return Result.Failure<Order>(OrderErrors.NegativeTotalAmount);
        }

        // For PendingPayment status, we require a payment intent ID and don't require payment transactions yet
        // They will be created after payment confirmation
        if (initialStatus == OrderStatus.PendingPayment)
        {
            // Require payment intent ID for PendingPayment status
            if (string.IsNullOrEmpty(paymentIntentId))
            {
                return Result.Failure<Order>(OrderErrors.PaymentIntentIdRequired);
            }
            
            // If status is PendingPayment, we shouldn't have any payment transactions yet
            if (paymentTransactions.Any())
            {
                paymentTransactions = new List<PaymentTransaction>();
            }
        }
        else
        {
            // Invariant Check 2 (Payment Integrity):
            // Calculates the sum of all `paymentTransactions` amounts.
            var totalPaid = paymentTransactions.Sum(p => p.Amount.Amount);
            
            // Invariant Check 3 (Payment Match):
            // Asserts that the sum of payments equals the `totalAmount`.
            // Use a small tolerance for floating-point comparisons.
            if (Math.Abs(totalPaid - totalAmount.Amount) > 0.01m)
            {
                return Result.Failure<Order>(OrderErrors.PaymentMismatch);
            }
        }

        var currentTimestamp = timestamp ?? DateTime.UtcNow;
        
        var order = new Order(
            OrderId.CreateUnique(),
            GenerateOrderNumber(currentTimestamp),
            customerId,
            restaurantId,
            deliveryAddress,
            orderItems,
            specialInstructions,
            subtotal,
            discountAmount,
            deliveryFee,
            tipAmount,
            taxAmount,
            totalAmount,
            paymentTransactions,
            appliedCouponId,
            sourceTeamCartId,
            initialStatus,
            paymentIntentId,
            currentTimestamp);

        order.AddDomainEvent(new OrderCreated(order.Id, order.CustomerId, order.RestaurantId, order.TotalAmount));

        return order;
    }

    #endregion

    #region Public Methods - Order Lifecycle

    /// <summary>
    /// Accepts the order, setting its status to <see cref="OrderStatus.Accepted"/>
    /// and recording the estimated delivery time.
    /// </summary>
    /// <param name="estimatedDeliveryTime">The estimated time when the order will be delivered.</param>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result Accept(DateTime estimatedDeliveryTime, DateTime? timestamp = null)
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForAccept);
        }

        Status = OrderStatus.Accepted;
        EstimatedDeliveryTime = estimatedDeliveryTime;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;

        AddDomainEvent(new OrderAccepted(Id));
        return Result.Success();
    }

    /// <summary>
    /// Rejects the order, setting its status to <see cref="OrderStatus.Rejected"/>.
    /// </summary>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result Reject(DateTime? timestamp = null)
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(OrderErrors.InvalidStatusForReject);
        }

        Status = OrderStatus.Rejected;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;

        AddDomainEvent(new OrderRejected(Id));
        return Result.Success();
    }

    /// <summary>
    /// Cancels the order, setting its status to <see cref="OrderStatus.Cancelled"/>.
    /// </summary>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result Cancel(DateTime? timestamp = null)
    {
        if (Status != OrderStatus.Placed &&
            Status != OrderStatus.Accepted &&
            Status != OrderStatus.Preparing &&
            Status != OrderStatus.ReadyForDelivery)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForCancel);
        }

        Status = OrderStatus.Cancelled;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;

        AddDomainEvent(new OrderCancelled(Id));
        return Result.Success();
    }

    /// <summary>
    /// Marks the order as preparing, setting its status to <see cref="OrderStatus.Preparing"/>.
    /// </summary>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result MarkAsPreparing(DateTime? timestamp = null)
    {
        if (Status != OrderStatus.Accepted)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForPreparing);
        }

        Status = OrderStatus.Preparing;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;

        AddDomainEvent(new OrderPreparing(Id));
        return Result.Success();
    }

    /// <summary>
    /// Marks the order as ready for delivery, setting its status to <see cref="OrderStatus.ReadyForDelivery"/>.
    /// </summary>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result MarkAsReadyForDelivery(DateTime? timestamp = null)
    {
        if (Status != OrderStatus.Preparing)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForReadyForDelivery);
        }

        Status = OrderStatus.ReadyForDelivery;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;

        AddDomainEvent(new OrderReadyForDelivery(Id));
        return Result.Success();
    }

    /// <summary>
    /// Marks the order as delivered, setting its status to <see cref="OrderStatus.Delivered"/>
    /// and recording the actual delivery time.
    /// </summary>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result MarkAsDelivered(DateTime? timestamp = null)
    {
        if (Status != OrderStatus.ReadyForDelivery)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForDelivered);
        }

        var currentTimestamp = timestamp ?? DateTime.UtcNow;
        Status = OrderStatus.Delivered;
        ActualDeliveryTime = currentTimestamp;
        LastUpdateTimestamp = currentTimestamp;

        AddDomainEvent(new OrderDelivered(Id, ActualDeliveryTime.Value));
        return Result.Success();
    }

    /// <summary>
    /// Confirms payment for an order that was in PendingPayment status.
    /// This method is called by the payment webhook handler when payment is successful.
    /// </summary>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result ConfirmPayment(DateTime? timestamp = null)
    {
        if (Status != OrderStatus.PendingPayment)
        {
            return Result.Failure(OrderErrors.InvalidStatusForPaymentConfirmation);
        }

        Status = OrderStatus.Placed;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;

        AddDomainEvent(new OrderPaymentSucceeded(Id));
        return Result.Success();
    }

    /// <summary>
    /// Marks an order as having a failed payment.
    /// This method is called by the payment webhook handler when payment fails.
    /// </summary>
    /// <param name="timestamp">The timestamp when this action occurred.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public Result MarkAsPaymentFailed(DateTime? timestamp = null)
    {
        if (Status != OrderStatus.PendingPayment)
        {
            return Result.Failure(OrderErrors.InvalidStatusForPaymentConfirmation);
        }

        Status = OrderStatus.PaymentFailed;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;

        AddDomainEvent(new OrderPaymentFailed(Id));
        return Result.Success();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Generates a unique order number.
    /// </summary>
    /// <param name="timestamp">The timestamp to use for generating the order number.</param>
    /// <returns>A unique string representing the order number.</returns>
    private static string GenerateOrderNumber(DateTime timestamp)
    {
        // Format: ORD-YYYYMMDD-HHMMSS-XXXX (where XXXX is random)
        var datePart = timestamp.ToString("yyyyMMdd");
        var timePart = timestamp.ToString("HHmmss");
        var randomPart = Random.Shared.Next(1000, 9999);
        
        return $"ORD-{datePart}-{timePart}-{randomPart}";
    }

    #endregion
}
