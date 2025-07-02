using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
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
        
        Subtotal = CalculateSubtotal();
        TotalAmount = CalculateTotalAmount();
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
        {            return Result.Failure<Order>(OrderErrors.OrderItemRequired);
        }

        var order = new Order(
            OrderId.CreateUnique(),
            GenerateOrderNumber(),
            customerId,
            restaurantId,
            deliveryAddress,
            specialInstructions,
            discountAmount ?? Money.Zero,
            deliveryFee ?? Money.Zero,
            tipAmount ?? Money.Zero,
            taxAmount ?? Money.Zero,
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

    private Money CalculateSubtotal()
    {
        return new Money(_orderItems.Sum(item => item.LineItemTotal.Amount));
    }

    private Money CalculateTotalAmount()
    {
        return new Money(Subtotal.Amount - DiscountAmount.Amount + TaxAmount.Amount + DeliveryFee.Amount + TipAmount.Amount);
    }

    private static string GenerateOrderNumber()
    {
        // This is a simplistic approach. In a real system, this would be more robust.
        return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
    }

#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618
}
