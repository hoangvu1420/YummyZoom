using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Entities;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
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
    /// <summary>
    /// Converts a TeamCart to an Order with the provided delivery details.
    /// </summary>
    /// <param name="teamCart">The TeamCart to convert.</param>
    /// <param name="deliveryAddress">The delivery address for the order.</param>
    /// <param name="specialInstructions">Special instructions for the order.</param>
    /// <returns>A tuple containing the created Order and updated TeamCart.</returns>
    public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(
        TeamCart teamCart,
        DeliveryAddress deliveryAddress,
        string specialInstructions)
    {
        // 1. Validate the TeamCart's state
        if (teamCart.Status != TeamCartStatus.ReadyToConfirm)
        {
            return Result.Failure<(Order, TeamCart)>(TeamCartErrors.InvalidStatusForConversion);
        }

        if (teamCart.Items.Count == 0)
        {
            return Result.Failure<(Order, TeamCart)>(TeamCartErrors.ConversionDataIncomplete);
        }

        if (teamCart.MemberPayments.Count == 0)
        {
            return Result.Failure<(Order, TeamCart)>(TeamCartErrors.CannotConvertWithoutPayments);
        }

        // 2. Map TeamCartItems to OrderItems
        var orderItems = new List<OrderItem>();
        
        foreach (var cartItem in teamCart.Items)
        {
            var customizations = new List<OrderItemCustomization>();
            
            foreach (var customization in cartItem.SelectedCustomizations)
            {
                var customizationResult = OrderItemCustomization.Create(
                    customization.Snapshot_CustomizationGroupName,
                    customization.Snapshot_ChoiceName,
                    customization.Snapshot_ChoicePriceAdjustmentAtOrder);

                if (customizationResult.IsFailure)
                {
                    return Result.Failure<(Order, TeamCart)>(customizationResult.Error);
                }

                customizations.Add(customizationResult.Value);
            }

            var orderItemResult = OrderItem.Create(
                cartItem.Snapshot_MenuCategoryId,
                cartItem.Snapshot_MenuItemId,
                cartItem.Snapshot_ItemName,
                cartItem.Snapshot_BasePriceAtOrder,
                cartItem.Quantity,
                customizations.Any() ? customizations : null);

            if (orderItemResult.IsFailure)
            {
                return Result.Failure<(Order, TeamCart)>(orderItemResult.Error);
            }

            orderItems.Add(orderItemResult.Value);
        }

        // 3. Map MemberPayments to PaymentTransactions
        var paymentTransactions = CreatePaymentTransactionsFrom(teamCart);

        // 4. Create the Order using the enhanced factory method
        var orderResult = Order.Create(
            teamCart.HostUserId,
            teamCart.RestaurantId,
            deliveryAddress,
            orderItems,
            specialInstructions,
            discountAmount: teamCart.DiscountAmount,
            deliveryFee: null, // Delivery fee might be calculated at the last minute
            tipAmount: teamCart.TipAmount,
            taxAmount: null, // Tax is typically calculated later
            appliedCouponIds: teamCart.AppliedCouponId is not null ? new List<CouponId> { teamCart.AppliedCouponId } : null,
            sourceTeamCartId: teamCart.Id,
            paymentTransactions: paymentTransactions);

        if (orderResult.IsFailure)
        {
            return Result.Failure<(Order, TeamCart)>(orderResult.Error);
        }

        // 5. Mark the TeamCart as converted
        var conversionResult = teamCart.MarkAsConverted();
        if (conversionResult.IsFailure)
        {
            return Result.Failure<(Order, TeamCart)>(conversionResult.Error);
        }

        // 6. Raise the final conversion event
        var order = orderResult.Value;
        teamCart.AddDomainEvent(new TeamCartConverted(teamCart.Id, order.Id, DateTime.UtcNow, teamCart.HostUserId));

        return (order, teamCart);
    }

    /// <summary>
    /// Creates PaymentTransaction entities from TeamCart MemberPayments.
    /// </summary>
    /// <param name="teamCart">The TeamCart containing payment information.</param>
    /// <returns>A list of PaymentTransaction entities.</returns>
    private List<PaymentTransaction> CreatePaymentTransactionsFrom(TeamCart teamCart)
    {
        var transactions = new List<PaymentTransaction>();

        // Calculate the base total from member payments
        var baseTotal = teamCart.MemberPayments.Sum(p => p.Amount.Amount);
        
        // Calculate the adjusted total including tip and discount
        var adjustedTotal = baseTotal + teamCart.TipAmount.Amount - teamCart.DiscountAmount.Amount;
        
        // Calculate adjustment factor to distribute tip and discount proportionally
        var adjustmentFactor = baseTotal > 0 ? adjustedTotal / baseTotal : 1;

        // Handle online payments - each member gets their own transaction
        var onlinePayments = teamCart.MemberPayments
            .Where(p => p.Method == PaymentMethod.Online && p.Status == TeamCartAggregate.Enums.PaymentStatus.PaidOnline)
            .ToList();

        foreach (var payment in onlinePayments)
        {
            // Adjust payment amount proportionally
            var adjustedAmount = Math.Round(payment.Amount.Amount * adjustmentFactor, 2);
            
            var transaction = PaymentTransaction.Create(
                PaymentMethodType.CreditCard, // Use CreditCard as representative of online payments
                PaymentTransactionType.Payment,
                new Money(adjustedAmount, payment.Amount.Currency),
                DateTime.UtcNow,
                paymentMethodDisplay: "Online Payment",
                paymentGatewayReferenceId: payment.OnlineTransactionId,
                paidByUserId: payment.UserId).Value; // Assuming success for domain service

            transactions.Add(transaction);
        }

        // Handle COD payments - single transaction for all COD payments
        var codPayments = teamCart.MemberPayments
            .Where(p => p.Method == PaymentMethod.CashOnDelivery && p.Status == TeamCartAggregate.Enums.PaymentStatus.CommittedToCOD)
            .ToList();

        if (codPayments.Any())
        {
            // Calculate adjusted COD amount
            var baseCodAmount = codPayments.Sum(p => p.Amount.Amount);
            var adjustedCodAmount = Math.Round(baseCodAmount * adjustmentFactor, 2);
            
            var transaction = PaymentTransaction.Create(
                PaymentMethodType.CashOnDelivery,
                PaymentTransactionType.Payment,
                new Money(adjustedCodAmount, Currencies.Default),
                DateTime.UtcNow,
                paymentMethodDisplay: "Cash on Delivery",
                paymentGatewayReferenceId: null,
                paidByUserId: teamCart.HostUserId).Value; // Host is guarantor for COD

            transactions.Add(transaction);
        }

        return transactions;
    }
}
