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
    /// <param name="coupon">The full Coupon object, if one was applied.</param>
    /// <param name="currentUserCouponUsageCount">For validation.</param>
    /// <param name="deliveryFee">The delivery fee for the order.</param>
    /// <param name="taxAmount">The tax amount for the order.</param>
    /// <returns>A tuple containing the created Order and updated TeamCart.</returns>
    public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(
        TeamCart teamCart,
        DeliveryAddress deliveryAddress,
        string specialInstructions,
        Coupon? coupon,
        int currentUserCouponUsageCount,
        Money deliveryFee,
        Money taxAmount)
    {
        Console.WriteLine("\n--- [DEBUG] Starting ConvertToOrder ---");
        Console.WriteLine($"[DEBUG] TeamCart Status: {teamCart.Status}");
        Console.WriteLine($"[DEBUG] Expected Status: {TeamCartStatus.ReadyToConfirm}");
        Console.WriteLine($"[DEBUG] Status Check Result: {teamCart.Status != TeamCartStatus.ReadyToConfirm}");
        
        // 1. Validate State
        if (teamCart.Status != TeamCartStatus.ReadyToConfirm)
        {
            Console.WriteLine($"[DEBUG] !!! Status validation FAILED. Returning InvalidStatusForConversion error.");
            return Result.Failure<(Order, TeamCart)>(TeamCartErrors.InvalidStatusForConversion);
        }
        
        Console.WriteLine("[DEBUG] Status validation PASSED. Proceeding with conversion...");
        
        // 2. Map TeamCartItems to OrderItems
        var orderItems = MapToOrderItems(teamCart.Items);

        // 3. Perform All Financial Calculations using OrderFinancialService
        var subtotal = _financialService.CalculateSubtotal(orderItems);
        Console.WriteLine($"[DEBUG] Calculated Subtotal: {subtotal.Amount}");

        Money discountAmount = Money.Zero(subtotal.Currency);
        
        if (coupon is not null && teamCart.AppliedCouponId is not null && teamCart.AppliedCouponId == coupon.Id)
        {
            var discountResult = _financialService.ValidateAndCalculateDiscount(
                coupon,
                currentUserCouponUsageCount,
                orderItems,
                subtotal);

            if (discountResult.IsFailure)
            {
                Console.WriteLine($"[DEBUG] !!! Coupon validation FAILED. Error: {discountResult.Error.Code}");
                // Pass the coupon validation error directly from the financial service
                return Result.Failure<(Order, TeamCart)>(discountResult.Error);
            }
            discountAmount = discountResult.Value;
            Console.WriteLine($"[DEBUG] Coupon applied. Discount Amount: {discountAmount.Amount}");
        }

        var totalAmount = _financialService.CalculateFinalTotal(
            subtotal, 
            discountAmount, 
            deliveryFee, 
            teamCart.TipAmount, 
            taxAmount);
        Console.WriteLine($"[DEBUG] Calculated Final Order Total: {totalAmount.Amount}");

        // 4. Create Succeeded PaymentTransactions for the Order
        Console.WriteLine("[DEBUG] ==> Calling CreateSucceededPaymentTransactions...");
        var paymentTransactionsResult = CreateSucceededPaymentTransactions(teamCart, totalAmount);
        Console.WriteLine($"[DEBUG] <== CreateSucceededPaymentTransactions Result: IsFailure={paymentTransactionsResult.IsFailure}");
        if (paymentTransactionsResult.IsFailure)
        {
            Console.WriteLine($"[DEBUG] !!! Conversion failed at CreateSucceededPaymentTransactions. Error: {paymentTransactionsResult.Error.Code}");
            return Result.Failure<(Order, TeamCart)>(paymentTransactionsResult.Error);
        }

        // 5. Create the Order using the new, correct overload
        Console.WriteLine("[DEBUG] ==> Calling Order.Create...");
        var orderResult = Order.Create(
            teamCart.HostUserId,
            teamCart.RestaurantId,
            deliveryAddress,
            orderItems,
            specialInstructions,
            subtotal,
            discountAmount,
            deliveryFee,
            teamCart.TipAmount,
            taxAmount,
            totalAmount,
            paymentTransactionsResult.Value, 
            teamCart.AppliedCouponId,
            OrderStatus.Placed,
            sourceTeamCartId: teamCart.Id);

        Console.WriteLine($"[DEBUG] <== Order.Create Result: IsFailure={orderResult.IsFailure}");
        if (orderResult.IsFailure)
        {
            Console.WriteLine($"[DEBUG] !!! Conversion failed at Order.Create. Error: {orderResult.Error.Code}");
            return Result.Failure<(Order, TeamCart)>(orderResult.Error);
        }

        // 6. Finalize TeamCart State
        Console.WriteLine("[DEBUG] ==> Calling teamCart.MarkAsConverted...");
        var conversionResult = teamCart.MarkAsConverted();
        Console.WriteLine($"[DEBUG] <== teamCart.MarkAsConverted Result: IsFailure={conversionResult.IsFailure}");
        if (conversionResult.IsFailure)
        {
            Console.WriteLine($"[DEBUG] !!! Conversion failed at MarkAsConverted. Error: {conversionResult.Error.Code}");
            return Result.Failure<(Order, TeamCart)>(conversionResult.Error);
        }

        var order = orderResult.Value;
        teamCart.AddDomainEvent(new TeamCartConverted(teamCart.Id, order.Id, DateTime.UtcNow, teamCart.HostUserId));

        Console.WriteLine("[DEBUG] --- Conversion Succeeded ---");
        return (order, teamCart);
    }

    /// <summary>
    /// Creates PaymentTransaction entities from TeamCart MemberPayments.
    /// </summary>
    private Result<List<PaymentTransaction>> CreateSucceededPaymentTransactions(
        TeamCart teamCart, 
        Money totalAmount)
    {
        Console.WriteLine("\n--- [DEBUG] Inside CreateSucceededPaymentTransactions ---");
        Console.WriteLine($"[DEBUG] Target Order Total: {totalAmount.Amount}");
        
        var transactions = new List<PaymentTransaction>();
        
        // Check if MemberPayments is null or empty
        if (teamCart.MemberPayments is null || !teamCart.MemberPayments.Any())
        {
            Console.WriteLine("[DEBUG] !!! No member payments found. Returning CannotConvertWithoutPayments error.");
            return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.CannotConvertWithoutPayments);
        }
        
        Console.WriteLine($"[DEBUG] Found {teamCart.MemberPayments.Count} member payments");
        
        // Check if totalAmount is null
        if (totalAmount is null)
        {
            Console.WriteLine("[DEBUG] !!! Total amount is null. Returning FinalPaymentMismatch error.");
            return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.FinalPaymentMismatch);
        }
        
        var totalPaidByMembers = teamCart.MemberPayments.Sum(p => p.Amount.Amount);
        Console.WriteLine($"[DEBUG] Sum of Member Payments: {totalPaidByMembers}");
        
        var adjustmentFactor = totalPaidByMembers > 0 ? totalAmount.Amount / totalPaidByMembers : 1;
        Console.WriteLine($"[DEBUG] Calculated Adjustment Factor: {adjustmentFactor}");

        foreach (var memberPayment in teamCart.MemberPayments)
        {
            // Check if memberPayment.Amount is null
            if (memberPayment.Amount is null)
            {
                Console.WriteLine("[DEBUG] !!! Member payment amount is null. Returning FinalPaymentMismatch error.");
                return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.FinalPaymentMismatch);
            }
            
            var adjustedAmount = new Money(memberPayment.Amount.Amount * adjustmentFactor, memberPayment.Amount.Currency);
            Console.WriteLine($"[DEBUG]   - Member paid {memberPayment.Amount.Amount}, adjusted to {adjustedAmount.Amount}");
            
            var paymentMethodType = memberPayment.Method == PaymentMethod.Online 
                ? PaymentMethodType.CreditCard
                : PaymentMethodType.CashOnDelivery;

            var transactionResult = PaymentTransaction.Create(
                paymentMethodType,
                PaymentTransactionType.Payment,
                adjustedAmount,
                DateTime.UtcNow,
                paymentGatewayReferenceId: memberPayment.OnlineTransactionId,
                paidByUserId: memberPayment.UserId);
                
            if(transactionResult.IsFailure) 
            {
                Console.WriteLine($"[DEBUG] !!! PaymentTransaction.Create failed. Error: {transactionResult.Error.Code}");
                return Result.Failure<List<PaymentTransaction>>(transactionResult.Error);
            }

            var transaction = transactionResult.Value;
            transaction.MarkAsSucceeded();
            transactions.Add(transaction);
        }
        
        var finalTransactionSum = transactions.Sum(t => t.Amount.Amount);
        var difference = Math.Abs(finalTransactionSum - totalAmount.Amount);
        Console.WriteLine($"[DEBUG] Final Sum of Adjusted Transactions: {finalTransactionSum}");
        Console.WriteLine($"[DEBUG] Difference from Target: {difference}");
        
        if (difference > 0.01m)
        {
            Console.WriteLine("[DEBUG] !!! Mismatch DETECTED. Returning failure.");
            return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.FinalPaymentMismatch);
        }

        Console.WriteLine("[DEBUG] Mismatch NOT detected. Returning success.");
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
                    customization.Snapshot_ChoicePriceAdjustmentAtOrder);

                if (customizationResult.IsSuccess)
                {
                    customizations.Add(customizationResult.Value);
                }
            }

            var orderItemResult = OrderItem.Create(
                cartItem.Snapshot_MenuCategoryId,
                cartItem.Snapshot_MenuItemId,
                cartItem.Snapshot_ItemName,
                cartItem.Snapshot_BasePriceAtOrder,
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
