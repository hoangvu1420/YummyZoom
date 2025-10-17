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
        DateTime timestamp)
        : base(orderId)
    {
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
    /// Creates a new order for a standard single-payment flow (online or COD),
    /// where the order itself is responsible for creating the initial payment transaction.
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
        PaymentMethodType paymentMethodType,
        CouponId? appliedCouponId,
        string? paymentGatewayReferenceId = null,
        TeamCartId? sourceTeamCartId = null,
        DateTime? timestamp = null)
    {
        return Create(
            OrderId.CreateUnique(),
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
            paymentMethodType,
            appliedCouponId,
            paymentGatewayReferenceId,
            sourceTeamCartId,
            timestamp);
    }

    /// <summary>
    /// Creates a new order for a standard single-payment flow (online or COD) with a specific OrderId,
    /// where the order itself is responsible for creating the initial payment transaction.
    /// </summary>
    public static Result<Order> Create(
        OrderId orderId,
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
        PaymentMethodType paymentMethodType,
        CouponId? appliedCouponId,
        string? paymentGatewayReferenceId = null,
        TeamCartId? sourceTeamCartId = null,
        DateTime? timestamp = null)
    {
        if (!orderItems.Any())
        {
            return Result.Failure<Order>(OrderErrors.OrderItemRequired);
        }

        if (deliveryAddress is null)
        {
            return Result.Failure<Order>(OrderErrors.AddressInvalid);
        }

        var calculatedTotal = subtotal - discountAmount + deliveryFee + tipAmount + taxAmount; if (Math.Abs(calculatedTotal.Amount - totalAmount.Amount) > 0.01m)
        {
            return Result.Failure<Order>(OrderErrors.FinancialMismatch);
        }

        if (totalAmount.Amount < 0)
        {
            return Result.Failure<Order>(OrderErrors.NegativeTotalAmount);
        }

        var currentTimestamp = timestamp ?? DateTime.UtcNow;
        var paymentTransactions = new List<PaymentTransaction>();
        var initialStatus = OrderStatus.AwaitingPayment;

        if (paymentMethodType == PaymentMethodType.CashOnDelivery)
        {
            var codAmount = totalAmount.Copy();

            var codTransactionResult = PaymentTransaction.Create(
                PaymentMethodType.CashOnDelivery,
                PaymentTransactionType.Payment,
                codAmount,
                currentTimestamp
            );

            if (codTransactionResult.IsFailure)
            {
                return Result.Failure<Order>(codTransactionResult.Error);
            }

            codTransactionResult.Value.MarkAsSucceeded();
            paymentTransactions.Add(codTransactionResult.Value);
            initialStatus = OrderStatus.Placed;
        }
        else
        {
            if (string.IsNullOrEmpty(paymentGatewayReferenceId))
            {
                return Result.Failure<Order>(OrderErrors.PaymentGatewayReferenceIdRequired);
            }

            var txAmount = totalAmount.Copy();

            var onlinePaymentResult = PaymentTransaction.Create(
                paymentMethodType,
                PaymentTransactionType.Payment,
                txAmount,
                currentTimestamp,
                paymentMethodDisplay: paymentMethodType.ToString(),
                paymentGatewayReferenceId: paymentGatewayReferenceId
            );

            if (onlinePaymentResult.IsFailure)
            {
                return Result.Failure<Order>(onlinePaymentResult.Error);
            }

            paymentTransactions.Add(onlinePaymentResult.Value);
        }

        // This calls the Create method that accepts List<PaymentTransaction> internally for consistency
        return Create(
            orderId,
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
            initialStatus,
            sourceTeamCartId,
            currentTimestamp);
    }

    /// <summary>
    /// Creates a new order from a pre-validated set of data, including a list of payment transactions.
    /// This is ideal for trusted processes like TeamCart conversion.
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
        List<PaymentTransaction> paymentTransactions, // Accepts a pre-built list
        CouponId? appliedCouponId,
        OrderStatus initialStatus, // Accepts a pre-determined status
        TeamCartId? sourceTeamCartId = null,
        DateTime? timestamp = null)
    {
        return Create(
            OrderId.CreateUnique(),
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
            initialStatus,
            sourceTeamCartId,
            timestamp);
    }

    /// <summary>
    /// Creates a new order from a pre-validated set of data with a specific OrderId, including a list of payment transactions.
    /// This is ideal for trusted processes like TeamCart conversion.
    /// </summary>
    public static Result<Order> Create(
        OrderId orderId,
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
        OrderStatus initialStatus,
        TeamCartId? sourceTeamCartId = null,
        DateTime? timestamp = null)
    {
        if (!orderItems.Any())
        {
            return Result.Failure<Order>(OrderErrors.OrderItemRequired);
        }

        if (deliveryAddress is null)
        {
            return Result.Failure<Order>(OrderErrors.AddressInvalid);
        }

        var calculatedTotal = subtotal - discountAmount + deliveryFee + tipAmount + taxAmount;
        if (Math.Abs(calculatedTotal.Amount - totalAmount.Amount) > 0.01m)
        {
            return Result.Failure<Order>(OrderErrors.FinancialMismatch);
        }

        if (totalAmount.Amount < 0)
        {
            return Result.Failure<Order>(OrderErrors.NegativeTotalAmount);
        }

        var totalPaid = paymentTransactions.Sum(p => p.Amount.Amount);
        if (Math.Abs(totalPaid - totalAmount.Amount) > 0.01m)
        {
            return Result.Failure<Order>(OrderErrors.PaymentMismatch);
        }

        var currentTimestamp = timestamp ?? DateTime.UtcNow;

        var order = new Order(
            orderId,
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
            currentTimestamp);

        order.AddDomainEvent(new OrderCreated(order.Id, order.CustomerId, order.RestaurantId, order.TotalAmount));
        if (initialStatus == OrderStatus.Placed)
        {
            // Lifecycle event emitted when order starts in Placed status (e.g., COD)
            order.AddDomainEvent(new OrderPlaced(order.Id));
        }

        return order;
    }

    #endregion

    #region Public Methods - Order Lifecycle

    /// <summary>
    /// Accepts the order, setting its status to <see cref="OrderStatus.Accepted"/>
    /// and recording the estimated delivery time.
    /// </summary>
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
    /// Records a successful payment transaction, moving the Order to Placed status.
    /// </summary>
    public Result RecordPaymentSuccess(string paymentGatewayReferenceId, DateTime? timestamp = null)
    {
        if (Status != OrderStatus.AwaitingPayment)
        {
            return Result.Failure(OrderErrors.InvalidStatusForPaymentConfirmation);
        }

        var transaction = _paymentTransactions.FirstOrDefault(p => p.PaymentGatewayReferenceId == paymentGatewayReferenceId);
        if (transaction is null)
        {
            return Result.Failure(OrderErrors.PaymentTransactionNotFound);
        }

        transaction.MarkAsSucceeded();

        Status = OrderStatus.Placed;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;
        // Payment event + lifecycle event
        AddDomainEvent(new OrderPaymentSucceeded(Id));
        AddDomainEvent(new OrderPlaced(Id));
        return Result.Success();
    }

    /// <summary>
    /// Records a failed payment transaction, moving the Order to Cancelled status.
    /// </summary>
    public Result RecordPaymentFailure(string paymentGatewayReferenceId, DateTime? timestamp = null)
    {
        if (Status != OrderStatus.AwaitingPayment)
        {
            return Result.Failure(OrderErrors.InvalidStatusForPaymentConfirmation);
        }

        var transaction = _paymentTransactions.FirstOrDefault(p => p.PaymentGatewayReferenceId == paymentGatewayReferenceId);
        if (transaction is null)
        {
            return Result.Failure(OrderErrors.PaymentTransactionNotFound);
        }

        transaction.MarkAsFailed();

        Status = OrderStatus.Cancelled;
        LastUpdateTimestamp = timestamp ?? DateTime.UtcNow;
        // Payment failure also triggers cancellation lifecycle event
        AddDomainEvent(new OrderPaymentFailed(Id));
        AddDomainEvent(new OrderCancelled(Id));
        return Result.Success();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Generates a unique order number.
    /// </summary>
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
