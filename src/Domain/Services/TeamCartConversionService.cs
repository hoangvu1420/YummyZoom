using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.Events;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.Services;

/// <summary>
/// Domain service responsible for converting a TeamCart to an Order.
/// This service orchestrates the conversion process while respecting aggregate boundaries.
/// </summary>
public sealed class TeamCartConversionService
{
    private readonly OrderFinancialService _financialService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCartConversionService"/> class.
    /// </summary>
    /// <param name="financialService">The financial service for calculating order totals and discounts.</param>
    public TeamCartConversionService(OrderFinancialService financialService)
    {
        _financialService = financialService;
    }

    /// <summary>
    /// Converts a TeamCart to an Order with the provided delivery details.
    /// </summary>
    /// <param name="teamCart">The TeamCart to convert.</param>
    /// <param name="deliveryAddress">The delivery address for the order.</param>
    /// <param name="specialInstructions">Special instructions for the order.</param>
    /// <param name="coupon">The full Coupon object, if one was applied. Optional; only used to ensure consistency with TeamCart's applied coupon.</param>
    /// <param name="discountAmount">Precomputed discount amount to apply. Must be computed and validated by the Application layer.</param>
    /// <param name="deliveryFee">The delivery fee for the order.</param>
    /// <param name="taxAmount">The tax amount for the order.</param>
    /// <returns>A tuple containing the created Order and updated TeamCart.</returns>
    public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(
        TeamCart teamCart,
        DeliveryAddress deliveryAddress,
        string specialInstructions,
        Coupon? coupon,
        Money discountAmount,
        Money deliveryFee,
        Money taxAmount)
    {
        // 1. Validate State
        if (teamCart.Status != TeamCartStatus.ReadyToConfirm)
        {
            return Result.Failure<(Order, TeamCart)>(TeamCartErrors.InvalidStatusForConversion);
        }

        // 2. Map TeamCartItems to OrderItems
        var orderItems = MapToOrderItems(teamCart.Items);

        // 3. Perform All Financial Calculations using OrderFinancialService
        var subtotal = _financialService.CalculateSubtotal(orderItems);

        // The discount must be provided by the caller (Application layer) after performing
        // all necessary coupon validations and atomic usage increments. Here we ensure
        // the provided discount currency aligns with subtotal.
        if (discountAmount.Currency != subtotal.Currency)
        {
            discountAmount = new Money(discountAmount.Amount, subtotal.Currency);
        }

        var totalAmount = _financialService.CalculateFinalTotal(
            subtotal,
            discountAmount,
            deliveryFee,
            teamCart.TipAmount.Copy(), // Use copy to avoid EF Core tracking conflicts
            taxAmount);

        // 4. Create Succeeded PaymentTransactions for the Order
        var paymentTransactionsResult = CreateSucceededPaymentTransactions(teamCart, totalAmount);
        if (paymentTransactionsResult.IsFailure)
        {
            return Result.Failure<(Order, TeamCart)>(paymentTransactionsResult.Error);
        }

        // 5. Create the Order using the new, correct overload
        var orderResult = Order.Create(
            teamCart.HostUserId,
            teamCart.RestaurantId,
            deliveryAddress,
            orderItems,
            specialInstructions,
            subtotal,
            discountAmount,
            deliveryFee,
            teamCart.TipAmount.Copy(), // Use copy to avoid EF Core tracking conflicts
            taxAmount,
            totalAmount,
            paymentTransactionsResult.Value,
            teamCart.AppliedCouponId,
            OrderStatus.Placed,
            sourceTeamCartId: teamCart.Id);

        if (orderResult.IsFailure)
        {
            return Result.Failure<(Order, TeamCart)>(orderResult.Error);
        }

        // 6. Finalize TeamCart State
        var conversionResult = teamCart.MarkAsConverted();
        if (conversionResult.IsFailure)
        {
            return Result.Failure<(Order, TeamCart)>(conversionResult.Error);
        }

        var order = orderResult.Value;
        teamCart.AddDomainEvent(new TeamCartConverted(teamCart.Id, order.Id, DateTime.UtcNow, teamCart.HostUserId));

        return (order, teamCart);
    }

    /// <summary>
    /// Creates PaymentTransaction entities from TeamCart MemberPayments.
    /// </summary>
    private Result<List<PaymentTransaction>> CreateSucceededPaymentTransactions(
        TeamCart teamCart,
        Money totalAmount)
    {
        var transactions = new List<PaymentTransaction>();

        // Check if MemberPayments is null or empty
        if (teamCart.MemberPayments is null || !teamCart.MemberPayments.Any())
        {
            return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.CannotConvertWithoutPayments);
        }

        // Check if totalAmount is null
        if (totalAmount is null)
        {
            return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.FinalPaymentMismatch);
        }

        var totalPaidByMembers = teamCart.MemberPayments.Sum(p => p.Amount.Amount);

        var adjustmentFactor = totalPaidByMembers > 0 ? totalAmount.Amount / totalPaidByMembers : 1;

        foreach (var memberPayment in teamCart.MemberPayments)
        {
            // Check if memberPayment.Amount is null
            if (memberPayment.Amount is null)
            {
                return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.FinalPaymentMismatch);
            }

            var adjustedAmount = new Money(memberPayment.Amount.Amount * adjustmentFactor, memberPayment.Amount.Currency);

            var paymentMethodType = memberPayment.Method == PaymentMethod.Online
                ? PaymentMethodType.CreditCard
                : PaymentMethodType.CashOnDelivery;

            var transactionResult = PaymentTransaction.Create(
                paymentMethodType,
                PaymentTransactionType.Payment,
                adjustedAmount,
                DateTime.UtcNow,
                paymentMethodDisplay: paymentMethodType.ToString(), 
                paymentGatewayReferenceId: memberPayment.OnlineTransactionId,
                paidByUserId: memberPayment.UserId);

            if (transactionResult.IsFailure)
            {
                return Result.Failure<List<PaymentTransaction>>(transactionResult.Error);
            }

            var transaction = transactionResult.Value;
            transaction.MarkAsSucceeded();
            transactions.Add(transaction);
        }

        var finalTransactionSum = transactions.Sum(t => t.Amount.Amount);
        var difference = Math.Abs(finalTransactionSum - totalAmount.Amount);

        if (difference > 0.01m)
        {
            return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.FinalPaymentMismatch);
        }

        return Result.Success(transactions);
    }

    /// <summary>
    /// Maps TeamCartItems to OrderItems.
    /// </summary>
    private List<OrderItem> MapToOrderItems(IReadOnlyList<TeamCartItem> cartItems)
    {
        var orderItems = new List<OrderItem>();

        foreach (var cartItem in cartItems)
        {
            var customizations = new List<OrderItemCustomization>();

            foreach (var customization in cartItem.SelectedCustomizations)
            {
                var customizationResult = OrderItemCustomization.Create(
                    customization.Snapshot_CustomizationGroupName,
                    customization.Snapshot_ChoiceName,
                    customization.Snapshot_ChoicePriceAdjustmentAtOrder.Copy()); // Use copy to avoid EF Core tracking conflicts

                if (customizationResult.IsSuccess)
                {
                    customizations.Add(customizationResult.Value);
                }
            }

            var orderItemResult = OrderItem.Create(
                cartItem.Snapshot_MenuCategoryId,
                cartItem.Snapshot_MenuItemId,
                cartItem.Snapshot_ItemName,
                cartItem.Snapshot_BasePriceAtOrder.Copy(), // Use copy to avoid EF Core tracking conflicts
                cartItem.Quantity,
                customizations.Any() ? customizations : null);

            if (orderItemResult.IsSuccess)
            {
                orderItems.Add(orderItemResult.Value);
            }
        }

        return orderItems;
    }
}
