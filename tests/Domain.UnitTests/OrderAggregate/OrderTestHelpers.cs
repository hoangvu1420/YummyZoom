using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.UnitTests.OrderAggregate;

public abstract class OrderTestHelpers
{
    protected static readonly UserId DefaultCustomerId = UserId.CreateUnique();
    protected static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    protected static readonly DeliveryAddress DefaultDeliveryAddress = CreateDefaultDeliveryAddress();
    protected static readonly List<OrderItem> DefaultOrderItems = CreateDefaultOrderItems();
    protected const string DefaultSpecialInstructions = "No special instructions";
    protected static readonly Money DefaultDiscountAmount = Money.Zero(Currencies.Default);
    protected static readonly Money DefaultDeliveryFee = new Money(5.00m, Currencies.Default);
    protected static readonly Money DefaultTipAmount = new Money(2.00m, Currencies.Default);
    protected static readonly Money DefaultTaxAmount = new Money(1.50m, Currencies.Default);

    protected static Order CreateValidOrder()
    {
        return Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount).Value;
    }

    protected static Order CreateAcceptedOrder()
    {
        var order = CreateValidOrder();
        var result = order.Accept(DateTime.UtcNow.AddHours(1));
        result.ShouldBeSuccessful(); // Ensure the transition was successful
        return order;
    }

    protected static Order CreatePreparingOrder()
    {
        var order = CreateAcceptedOrder();
        var result = order.MarkAsPreparing();
        result.ShouldBeSuccessful(); // Ensure the transition was successful
        return order;
    }

    protected static Order CreateReadyForDeliveryOrder()
    {
        var order = CreatePreparingOrder();
        var result = order.MarkAsReadyForDelivery();
        result.ShouldBeSuccessful(); // Ensure the transition was successful
        return order;
    }

    protected static DeliveryAddress CreateDefaultDeliveryAddress()
    {
        return DeliveryAddress.Create(
            "123 Main St",
            "Springfield",
            "IL",
            "62701",
            "USA").Value;
    }

    protected static List<OrderItem> CreateDefaultOrderItems()
    {
        var orderItem = OrderItem.Create(
            MenuCategoryId.CreateUnique(),
            MenuItemId.CreateUnique(),
            "Test Item",
            new Money(10.00m, Currencies.Default),
            2).Value;

        return new List<OrderItem> { orderItem };
    }

    protected static PaymentTransaction CreateValidPaymentTransaction()
    {
        return PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(25.50m, Currencies.Default),
            DateTime.UtcNow).Value;
    }

    #region Phase 4 Helper Methods - TeamCart Integration

    /// <summary>
    /// Creates an order from a TeamCart with minimal required parameters
    /// </summary>
    protected static Order CreateOrderFromTeamCart(TeamCartId teamCartId, List<PaymentTransaction> paymentTransactions)
    {
        return Order.Create(
            DefaultCustomerId,
            DefaultRestaurantId,
            DefaultDeliveryAddress,
            DefaultOrderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount,
            null, 
            teamCartId,
            paymentTransactions).Value;
    }

    /// <summary>
    /// Creates an order from a TeamCart with all parameters
    /// </summary>
    protected static Order CreateOrderFromTeamCartWithAllParameters(
        TeamCartId teamCartId,
        List<PaymentTransaction> paymentTransactions,
        UserId customerId,
        RestaurantId restaurantId,
        DeliveryAddress deliveryAddress,
        List<OrderItem> orderItems)
    {
        return Order.Create(
            customerId,
            restaurantId,
            deliveryAddress,
            orderItems,
            DefaultSpecialInstructions,
            DefaultDiscountAmount,
            DefaultDeliveryFee,
            DefaultTipAmount,
            DefaultTaxAmount,
            null, 
            teamCartId,
            paymentTransactions).Value;
    }

    /// <summary>
    /// Creates payment transactions that match the given total amount
    /// </summary>
    protected static List<PaymentTransaction> CreatePaymentTransactionsWithTotal(Money totalAmount)
    {
        var transactions = new List<PaymentTransaction>();
        
        // Split the total into multiple transactions
        var halfAmount = new Money(totalAmount.Amount / 2, totalAmount.Currency);
        var remainingAmount = new Money(totalAmount.Amount - halfAmount.Amount, totalAmount.Currency);
        
        var transaction1 = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            halfAmount,
            DateTime.UtcNow,
            "Visa ending in 1234",
            "stripe_pi_123",
            UserId.CreateUnique());
        transactions.Add(transaction1.Value);
        
        var transaction2 = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            remainingAmount,
            DateTime.UtcNow,
            "Mastercard ending in 5678",
            "stripe_pi_456",
            UserId.CreateUnique());
        transactions.Add(transaction2.Value);
        
        return transactions;
    }

    /// <summary>
    /// Creates payment transactions that do NOT match the given total amount
    /// </summary>
    protected static List<PaymentTransaction> CreatePaymentTransactionsWithMismatchedTotal(Money targetAmount)
    {
        var transactions = new List<PaymentTransaction>();
        
        // Create transactions that total to a different amount
        var mismatchedAmount = new Money(targetAmount.Amount + 10.00m, targetAmount.Currency);
        
        var transaction = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            mismatchedAmount,
            DateTime.UtcNow,
            "Visa ending in 1234",
            "stripe_pi_mismatch",
            UserId.CreateUnique());
        transactions.Add(transaction.Value);
        
        return transactions;
    }

    /// <summary>
    /// Creates COD payment transactions (single transaction with host as guarantor)
    /// </summary>
    protected static List<PaymentTransaction> CreateCODPaymentTransactions(Money totalAmount, UserId hostUserId)
    {
        var transactions = new List<PaymentTransaction>();
        
        var codTransaction = PaymentTransaction.Create(
            PaymentMethodType.CashOnDelivery,
            PaymentTransactionType.Payment,
            totalAmount,
            DateTime.UtcNow,
            "Cash on Delivery",
            null,
            hostUserId);
        transactions.Add(codTransaction.Value);
        
        return transactions;
    }

    /// <summary>
    /// Creates multiple payment transactions with different users (simulating TeamCart members)
    /// </summary>
    protected static List<PaymentTransaction> CreateMultiUserPaymentTransactions()
    {
        var transactions = new List<PaymentTransaction>();
        
        // Host payment
        var hostPayment = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(15.00m, Currencies.Default),
            DateTime.UtcNow,
            "Visa ending in 1234",
            "stripe_pi_host_123",
            UserId.CreateUnique());
        transactions.Add(hostPayment.Value);
        
        // Member 1 payment
        var member1Payment = PaymentTransaction.Create(
            PaymentMethodType.CreditCard,
            PaymentTransactionType.Payment,
            new Money(10.00m, Currencies.Default),
            DateTime.UtcNow,
            "Mastercard ending in 5678",
            "stripe_pi_member1_456",
            UserId.CreateUnique());
        transactions.Add(member1Payment.Value);
        
        // Member 2 payment
        var member2Payment = PaymentTransaction.Create(
            PaymentMethodType.ApplePay,
            PaymentTransactionType.Payment,
            new Money(3.50m, Currencies.Default),
            DateTime.UtcNow,
            "Apple Pay",
            "applepay_txn_789",
            UserId.CreateUnique());
        transactions.Add(member2Payment.Value);
        
        return transactions;
    }

    /// <summary>
    /// Calculates the total amount for default order items and fees
    /// </summary>
    protected static Money CalculateDefaultOrderTotal()
    {
        var itemsTotal = DefaultOrderItems.Sum(item => item.Snapshot_BasePriceAtOrder.Amount * item.Quantity);
        var total = itemsTotal + DefaultDeliveryFee.Amount + DefaultTipAmount.Amount + DefaultTaxAmount.Amount - DefaultDiscountAmount.Amount;
        return new Money(total, Currencies.Default);
    }

    /// <summary>
    /// Creates a sample TeamCartId for testing
    /// </summary>
    protected static TeamCartId CreateSampleTeamCartId()
    {
        return TeamCartId.CreateUnique();
    }

    #endregion
}
