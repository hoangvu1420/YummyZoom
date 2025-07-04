using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.Events;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate;

public sealed class Order : AggregateRoot<OrderId, Guid>
{
    private readonly List<OrderItem> _orderItems = [];
    private readonly List<PaymentTransaction> _paymentTransactions = [];
    private readonly List<CouponId> _appliedCouponIds = [];

    public new OrderId Id { get; private set; }

    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime PlacementTimestamp { get; private set; }
    public DateTime LastUpdateTimestamp { get; private set; }
    public DateTime? EstimatedDeliveryTime { get; private set; }
    public string SpecialInstructions { get; private set; }
    public DeliveryAddress DeliveryAddress { get; private set; }
    public Money Subtotal { get; private set; }
    public Money DiscountAmount { get; private set; }
    public Money DeliveryFee { get; private set; }
    public Money TipAmount { get; private set; }
    public Money TaxAmount { get; private set; }
    public Money TotalAmount { get; private set; }
    public UserId CustomerId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }

    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public IReadOnlyList<PaymentTransaction> PaymentTransactions => _paymentTransactions.AsReadOnly();
    public IReadOnlyList<CouponId> AppliedCouponIds => _appliedCouponIds.AsReadOnly();

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
        List<CouponId>? appliedCouponIds)
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
        _orderItems = orderItems;
        _appliedCouponIds = appliedCouponIds ?? new List<CouponId>();

        Status = OrderStatus.Placed;
        PlacementTimestamp = DateTime.UtcNow;
        LastUpdateTimestamp = DateTime.UtcNow;
        
        var orderCurrency = deliveryFee.Currency;

        Subtotal = Money.Zero(orderCurrency);
        TotalAmount = Money.Zero(orderCurrency);

        RecalculateTotals();
    }

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
        List<CouponId>? appliedCouponIds = null)
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
            appliedCouponIds);

        if (order.TotalAmount.Amount < 0)
        {
            return Result.Failure<Order>(OrderErrors.NegativeTotalAmount);
        }

        order.AddDomainEvent(new OrderCreated(order.Id, order.CustomerId, order.RestaurantId, order.TotalAmount));

        return order;
    }

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

    public Result Cancel()
    {
        if (Status != OrderStatus.Placed && Status != OrderStatus.Accepted)
        {
            return Result.Failure(OrderErrors.InvalidOrderStatusForCancel);
        }

        Status = OrderStatus.Cancelled;
        LastUpdateTimestamp = DateTime.UtcNow;

        AddDomainEvent(new OrderCancelled(Id));
        return Result.Success();
    }

    public Result AddPaymentAttempt(PaymentTransaction payment)
    {
        _paymentTransactions.Add(payment);
        return Result.Success();
    }

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

    /// <summary>
    /// Applies a coupon to the order using decoupled parameters (preferred approach)
    /// </summary>
    /// <param name="couponId">The ID of the coupon being applied</param>
    /// <param name="couponValue">The value and type of the coupon</param>
    /// <param name="appliesTo">The scope and criteria for coupon application</param>
    /// <param name="minOrderAmount">Minimum order amount required for coupon eligibility</param>
    /// <returns>Result indicating success or failure</returns>
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

        if (_appliedCouponIds.Any())
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
        _appliedCouponIds.Add(couponId);
        RecalculateTotals();
        LastUpdateTimestamp = DateTime.UtcNow;

        return Result.Success();
    }

    public Result RemoveCoupon()
    {
        if (!_appliedCouponIds.Any())
        {
            return Result.Success(); // No coupon to remove
        }

        DiscountAmount = Money.Zero(Subtotal.Currency);
        _appliedCouponIds.Clear();
        RecalculateTotals();
        LastUpdateTimestamp = DateTime.UtcNow;

        return Result.Success();
    }

    private decimal GetDiscountBaseAmount(CouponValue couponValue, AppliesTo appliesTo)
    {
        // For FreeItem coupons, calculate based on the specific free item
        if (couponValue.Type == CouponType.FreeItem && couponValue.FreeItemValue is not null)
        {
            return GetFreeItemDiscountAmount(couponValue.FreeItemValue);
        }

        switch (appliesTo.Scope)
        {
            case CouponScope.WholeOrder:
                return Subtotal.Amount;

            case CouponScope.SpecificItems:
                return _orderItems
                    .Where(oi => appliesTo.ItemIds!.Contains(oi.Snapshot_MenuItemId))
                    .Sum(oi => oi.LineItemTotal.Amount);

            case CouponScope.SpecificCategories:
                return _orderItems
                    .Where(oi => appliesTo.CategoryIds!.Contains(oi.Snapshot_MenuCategoryId))
                    .Sum(oi => oi.LineItemTotal.Amount);

            default:
                return 0;
        }
    }

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

    private static string GenerateOrderNumber()
    {
        // Format: ORD-YYYYMMDD-HHMMSS-XXXX (where XXXX is random)
        var now = DateTime.UtcNow;
        var datePart = now.ToString("yyyyMMdd");
        var timePart = now.ToString("HHmmss");
        var randomPart = Random.Shared.Next(1000, 9999);
        
        return $"ORD-{datePart}-{timePart}-{randomPart}";
    }

#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618
}
