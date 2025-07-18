using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
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
    /// Gets the unique identifier of the order.
    /// </summary>
    public new OrderId Id { get; private set; }

    /// <summary>
    /// Gets the human-readable order number.
    /// </summary>
    public string OrderNumber { get; private set; }

    /// <summary>
    /// Gets the current status of the order.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Gets the timestamp when the order was placed.
    /// </summary>
    public DateTime PlacementTimestamp { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last update to the order.
    /// </summary>
    public DateTime LastUpdateTimestamp { get; private set; }

    /// <summary>
    /// Gets or sets the estimated delivery time for the order.
    /// </summary>
    public DateTime? EstimatedDeliveryTime { get; private set; }

    /// <summary>
    /// Gets or sets the actual delivery time for the order.
    /// </summary>
    public DateTime? ActualDeliveryTime { get; private set; }

    /// <summary>
    /// Gets special instructions provided by the customer for the order.
    /// </summary>
    public string SpecialInstructions { get; private set; }

    /// <summary>
    /// Gets the delivery address for the order.
    /// </summary>
    public DeliveryAddress DeliveryAddress { get; private set; }

    /// <summary>
    /// Gets the subtotal of the order before discounts, taxes, and fees.
    /// </summary>
    public Money Subtotal { get; private set; }

    /// <summary>
    /// Gets the total discount amount applied to the order.
    /// </summary>
    public Money DiscountAmount { get; private set; }

    /// <summary>
    /// Gets the delivery fee for the order.
    /// </summary>
    public Money DeliveryFee { get; private set; }

    /// <summary>
    /// Gets the tip amount for the order.
    /// </summary>
    public Money TipAmount { get; private set; }

    /// <summary>
    /// Gets the tax amount for the order.
    /// </summary>
    public Money TaxAmount { get; private set; }

    /// <summary>
    /// Gets the total amount of the order, including all items, discounts, taxes, and fees.
    /// </summary>
    public Money TotalAmount { get; private set; }

    /// <summary>
    /// Gets the ID of the customer who placed the order.
    /// </summary>
    public UserId CustomerId { get; private set; }

    /// <summary>
    /// Gets the ID of the restaurant from which the order was placed.
    /// </summary>
    public RestaurantId RestaurantId { get; private set; }

    /// <summary>
    /// Gets the ID of the source TeamCart if this order was created from a team cart.
    /// Null for regular individual orders.
    /// </summary>
    public TeamCartId? SourceTeamCartId { get; private set; }

    /// <summary>
    /// Gets the applied coupon IDs for this order.
    /// </summary>
    public CouponId? AppliedCouponId { get; private set; }

    /// <summary>
    /// Gets a read-only list of items included in the order.
    /// </summary>
    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();

    /// <summary>
    /// Gets a read-only list of payment transactions associated with the order.
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
        string specialInstructions,
        Money discountAmount,
        Money deliveryFee,
        Money tipAmount,
        Money taxAmount,
        List<OrderItem> orderItems,
        CouponId? appliedCouponId)
        : base(orderId)
    {
        Id = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        RestaurantId = restaurantId;
        DeliveryAddress = deliveryAddress;
        SpecialInstructions = specialInstructions;
        DiscountAmount = discountAmount;
        DeliveryFee = deliveryFee;
        TipAmount = tipAmount;
        TaxAmount = taxAmount;
        _orderItems = new List<OrderItem>(orderItems);
        AppliedCouponId = appliedCouponId;

        Status = OrderStatus.Placed;
        PlacementTimestamp = DateTime.UtcNow;
        LastUpdateTimestamp = DateTime.UtcNow;
        
        var orderCurrency = deliveryFee.Currency;

        Subtotal = Money.Zero(orderCurrency);
        TotalAmount = Money.Zero(orderCurrency);

        RecalculateTotals();
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
    /// Creates a new order instance.
    /// </summary>
    /// <param name="customerId">The ID of the customer placing the order.</param>
    /// <param name="restaurantId">The ID of the restaurant for the order.</param>
    /// <param name="deliveryAddress">The delivery address for the order.</param>
    /// <param name="orderItems">A list of items included in the order.</param>
    /// <param name="specialInstructions">Any special instructions for the order.</param>
    /// <param name="discountAmount">Optional discount amount applied.</param>
    /// <param name="deliveryFee">Optional delivery fee.</param>
    /// <param name="tipAmount">Optional tip amount.</param>
    /// <param name="taxAmount">Optional tax amount.</param>
    /// <param name="appliedCouponIds">Optional list of applied coupon IDs.</param>
    /// <param name="sourceTeamCartId">Optional ID of the team cart this order was created from.</param>
    /// <param name="paymentTransactions">Optional list of payment transactions for the order.</param>
    /// <returns>A <see cref="Result{Order}"/> indicating success or failure.</returns>
    public static Result<Order> Create(
        UserId customerId,
        RestaurantId restaurantId,
        DeliveryAddress deliveryAddress,
        List<OrderItem> orderItems,
        string specialInstructions,
        Money? discountAmount = null,
        Money? deliveryFee = null,
        Money? tipAmount = null,
        Money? taxAmount = null,
        CouponId? appliedCouponId = null,
        TeamCartId? sourceTeamCartId = null,
        List<PaymentTransaction>? paymentTransactions = null)
    {
        if (!orderItems.Any())
        {
            return Result.Failure<Order>(OrderErrors.OrderItemRequired);
        }

        // Establish a single currency for the new order from the items provided.
        var currency = orderItems.First().Snapshot_BasePriceAtOrder.Currency;

        var order = new Order(
            OrderId.CreateUnique(),
            GenerateOrderNumber(),
            customerId,
            restaurantId,
            deliveryAddress,
            specialInstructions,            
            discountAmount ?? Money.Zero(currency),
            deliveryFee ?? Money.Zero(currency),
            tipAmount ?? Money.Zero(currency),
            taxAmount ?? Money.Zero(currency),
            orderItems,
            appliedCouponId);

        // Set the source TeamCart ID if provided
        order.SourceTeamCartId = sourceTeamCartId;

        // Add payment transactions if provided
        if (paymentTransactions is not null && paymentTransactions.Any())
        {
            order._paymentTransactions.AddRange(paymentTransactions);
            
            // Ensure totals are calculated before validating payment
            order.RecalculateTotals();
            
            // Validate payment transactions total matches order total (round to 2 decimals to avoid precision issues)
            var totalPaid = order._paymentTransactions.Sum(p => p.Amount.Amount);
            var roundedPaid = Math.Round(totalPaid, 2);
            var roundedTotal = Math.Round(order.TotalAmount.Amount, 2);
            
            if (Math.Abs(roundedPaid - roundedTotal) > 0.01m)
            {
                return Result.Failure<Order>(OrderErrors.PaymentMismatch);
            }
        }

        if (order.TotalAmount.Amount < 0)
        {
            return Result.Failure<Order>(OrderErrors.NegativeTotalAmount);
        }

        order.AddDomainEvent(new OrderCreated(order.Id, order.CustomerId, order.RestaurantId, order.TotalAmount));

        return order;
    }

    #endregion

    #region Public Methods - Order Lifecycle

    /// <summary>
    /// Accepts the order, setting its status to <see cref="OrderStatus.Accepted"/> and
    /// providing an estimated delivery time.
    /// </summary>
    /// <param name="estimatedDeliveryTime">The estimated time for delivery.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result Accept(DateTime estimatedDeliveryTime)
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForAccept);
        }

        Status = OrderStatus.Accepted;
        EstimatedDeliveryTime = estimatedDeliveryTime;
        LastUpdateTimestamp = DateTime.UtcNow;

        AddDomainEvent(new OrderAccepted(Id));
        return Result.Success();
    }

    /// <summary>
    /// Rejects the order, setting its status to <see cref="OrderStatus.Rejected"/>.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result Reject()
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(OrderErrors.InvalidStatusForReject);
        }

        Status = OrderStatus.Rejected;
        LastUpdateTimestamp = DateTime.UtcNow;

        AddDomainEvent(new OrderRejected(Id));
        return Result.Success();
    }

    /// <summary>
    /// Cancels the order, setting its status to <see cref="OrderStatus.Cancelled"/>.
    /// Cancellation is allowed only for specific statuses.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result Cancel()
    {
        if (Status != OrderStatus.Placed && 
            Status != OrderStatus.Accepted && 
            Status != OrderStatus.Preparing && 
            Status != OrderStatus.ReadyForDelivery)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForCancel);
        }

        Status = OrderStatus.Cancelled;
        LastUpdateTimestamp = DateTime.UtcNow;

        AddDomainEvent(new OrderCancelled(Id));
        return Result.Success();
    }

    /// <summary>
    /// Marks the order as preparing, setting its status to <see cref="OrderStatus.Preparing"/>.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result MarkAsPreparing()
    {
        if (Status != OrderStatus.Accepted)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForPreparing);
        }

        Status = OrderStatus.Preparing;
        LastUpdateTimestamp = DateTime.UtcNow;

        AddDomainEvent(new OrderPreparing(Id));
        return Result.Success();
    }

    /// <summary>
    /// Marks the order as ready for delivery, setting its status to <see cref="OrderStatus.ReadyForDelivery"/>.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result MarkAsReadyForDelivery()
    {
        if (Status != OrderStatus.Preparing)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForReadyForDelivery);
        }

        Status = OrderStatus.ReadyForDelivery;
        LastUpdateTimestamp = DateTime.UtcNow;

        AddDomainEvent(new OrderReadyForDelivery(Id));
        return Result.Success();
    }

    /// <summary>
    /// Marks the order as delivered, setting its status to <see cref="OrderStatus.Delivered"/>
    /// and recording the actual delivery time.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result MarkAsDelivered()
    {
        if (Status != OrderStatus.ReadyForDelivery)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForDelivered);
        }

        Status = OrderStatus.Delivered;
        ActualDeliveryTime = DateTime.UtcNow;
        LastUpdateTimestamp = DateTime.UtcNow;

        AddDomainEvent(new OrderDelivered(Id, ActualDeliveryTime.Value));
        return Result.Success();
    }

    #endregion

    #region Public Methods - Payments

    /// <summary>
    /// Adds a payment attempt to the order.
    /// </summary>
    /// <param name="payment">The payment transaction to add.</param>
    /// <returns>A <see cref="Result"/> indicating success.</returns>
    public Result AddPaymentAttempt(PaymentTransaction payment)
    {
        _paymentTransactions.Add(payment);
        return Result.Success();
    }

    /// <summary>
    /// Marks a specific payment transaction as succeeded.
    /// </summary>
    /// <param name="paymentTransactionId">The ID of the payment transaction to mark as paid.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result MarkAsPaid(PaymentTransactionId paymentTransactionId)
    {
        var payment = _paymentTransactions.FirstOrDefault(p => p.Id == paymentTransactionId);
        if (payment is null)
        {
            return Result.Failure(OrderErrors.PaymentNotFound);
        }

        payment.MarkAsSucceeded();
        AddDomainEvent(new OrderPaid(Id, paymentTransactionId));
        return Result.Success();
    }

    #endregion

    #region Public Methods - Coupons

    /// <summary>
    /// Applies a coupon to the order using decoupled parameters (preferred approach).
    /// </summary>
    /// <param name="couponId">The ID of the coupon being applied.</param>
    /// <param name="couponValue">The value and type of the coupon.</param>
    /// <param name="appliesTo">The scope and criteria for coupon application.</param>
    /// <param name="minOrderAmount">Minimum order amount required for coupon eligibility.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result ApplyCoupon(
        CouponId couponId,
        CouponValue couponValue,
        AppliesTo appliesTo,
        Money? minOrderAmount)
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(OrderErrors.CouponCannotBeAppliedToOrderStatus);
        }

        if (AppliedCouponId is not null)
        {
            return Result.Failure(OrderErrors.CouponAlreadyApplied);
        }

        if (minOrderAmount is not null && Subtotal.Amount < minOrderAmount.Amount)
        {
            return Result.Failure(OrderErrors.CouponNotApplicable);
        }

        var discountBaseAmount = GetDiscountBaseAmount(couponValue, appliesTo);
        if (discountBaseAmount <= 0)
        {
            return Result.Failure(OrderErrors.CouponNotApplicable);
        }

        var discountBaseMoney = new Money(discountBaseAmount, Subtotal.Currency);
        Money newDiscount;
        switch (couponValue.Type)
        {
            case CouponType.Percentage:
                newDiscount = discountBaseMoney * (couponValue.PercentageValue!.Value / 100m);
                break;
            case CouponType.FixedAmount:
                // Discount cannot be more than the value of the items it applies to
                var fixedAmount = couponValue.FixedAmountValue!; // Assumes this is a Money object
                newDiscount = new Money(Math.Min(discountBaseMoney.Amount, fixedAmount.Amount), Subtotal.Currency);
                break;
            case CouponType.FreeItem:
                // For free item coupons, the discount is the price of one unit of the cheapest matching item
                newDiscount = discountBaseMoney;
                break;
            default:
                return Result.Failure(OrderErrors.CouponNotApplicable);
        }

        // Ensure discount doesn't exceed subtotal
        if (newDiscount.Amount > Subtotal.Amount) // Comparison operators could be added to Money VO
        {
            newDiscount = Subtotal;
        }

        DiscountAmount = newDiscount;
        AppliedCouponId = couponId;
        RecalculateTotals();
        LastUpdateTimestamp = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Removes any applied coupon from the order.
    /// </summary>
    /// <returns>A <see cref="Result"/> indicating success.</returns>
    public Result RemoveCoupon()
    {
        if (AppliedCouponId is null)
        {
            return Result.Success(); // No coupon to remove
        }

        DiscountAmount = Money.Zero(Subtotal.Currency);
        AppliedCouponId = null;
        RecalculateTotals();
        LastUpdateTimestamp = DateTime.UtcNow;

        return Result.Success();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Calculates the base amount on which the coupon discount will be applied.
    /// </summary>
    /// <param name="couponValue">The value and type of the coupon.</param>
    /// <param name="appliesTo">The scope and criteria for coupon application.</param>
    /// <returns>The decimal amount to base the discount calculation on.</returns>
    private decimal GetDiscountBaseAmount(CouponValue couponValue, AppliesTo appliesTo)
    {
        // For FreeItem coupons, calculate based on the specific free item
        if (couponValue is { Type: CouponType.FreeItem, FreeItemValue: not null })
        {
            return GetFreeItemDiscountAmount(couponValue.FreeItemValue);
        }

        switch (appliesTo.Scope)
        {
            case CouponScope.WholeOrder:
                return Subtotal.Amount;

            case CouponScope.SpecificItems:
                return _orderItems
                    .Where(oi => appliesTo.ItemIds.Contains(oi.Snapshot_MenuItemId))
                    .Sum(oi => oi.LineItemTotal.Amount);

            case CouponScope.SpecificCategories:
                return _orderItems
                    .Where(oi => appliesTo.CategoryIds.Contains(oi.Snapshot_MenuCategoryId))
                    .Sum(oi => oi.LineItemTotal.Amount);

            default:
                return 0;
        }
    }

    /// <summary>
    /// Calculates the discount amount for a free item coupon.
    /// </summary>
    /// <param name="freeItemId">The ID of the menu item that is free.</param>
    /// <returns>The decimal value representing the discount for the free item.</returns>
    private decimal GetFreeItemDiscountAmount(MenuItemId freeItemId)
    {
        // Find all order items that match the free item
        var matchingItems = _orderItems
            .Where(oi => oi.Snapshot_MenuItemId == freeItemId)
            .ToList();

        if (!matchingItems.Any())
        {
            return 0; // No matching items found
        }

        // For free item coupons, typically apply to the cheapest occurrence
        // Calculate per-unit price including customizations
        var cheapestItem = matchingItems
            .OrderBy(oi => oi.LineItemTotal.Amount / oi.Quantity)
            .First();

        // Return the price for one unit of the cheapest matching item
        return cheapestItem.LineItemTotal.Amount / cheapestItem.Quantity;
    }

    /// <summary>
    /// Recalculates the subtotal and total amount of the order based on current items, discounts, taxes, and fees.
    /// </summary>
    private void RecalculateTotals()
    {
        // If there are no items, totals are based on fees/tips alone.
        if (!_orderItems.Any())
        {
            var currency = DeliveryFee.Currency; // Get currency from another source
            Subtotal = Money.Zero(currency);
            TotalAmount = Subtotal - DiscountAmount + TaxAmount + DeliveryFee + TipAmount;
            return;
        }
        
        // Establish the currency for the calculation from the items.
        var orderCurrency = _orderItems.First().LineItemTotal.Currency;

        // 1. Calculate Subtotal from all items
        Subtotal = _orderItems.Sum(item => item.LineItemTotal, orderCurrency);

        // 2. Calculate final total
        TotalAmount = Subtotal - DiscountAmount + TaxAmount + DeliveryFee + TipAmount;
    }

    /// <summary>
    /// Generates a unique order number.
    /// </summary>
    /// <returns>A unique string representing the order number.</returns>
    private static string GenerateOrderNumber()
    {
        // Format: ORD-YYYYMMDD-HHMMSS-XXXX (where XXXX is random)
        var now = DateTime.UtcNow;
        var datePart = now.ToString("yyyyMMdd");
        var timePart = now.ToString("HHmmss");
        var randomPart = Random.Shared.Next(1000, 9999);
        
        return $"ORD-{datePart}-{timePart}-{randomPart}";
    }

    #endregion
    
}
