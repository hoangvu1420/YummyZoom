### The "Finalize & Pay" Hybrid Model: A Better Approach

The core idea is to separate the "Item Adding" phase from the "Payment" phase. The Host triggers this transition, which locks the items and financials, and then notifies everyone that it's time to pay.

#### **Revised `TeamCart` Lifecycle & Statuses**

1.  **`Open`:** The collaborative "shopping" phase.
    *   **Allowed Actions:** Members can join, add/edit/remove their own items. The Host can set a deadline.
    *   **Disallowed Actions:** No payments. No tips. No coupons. This is strictly for building the order.

2.  **`Locked`:** The "financial finalization and payment" phase.
    *   **Allowed Actions:**
        *   **Host Only:** Apply/remove a tip. Apply/remove a coupon.
        *   **All Members:** Make their payment (COD or Online).
    *   **Disallowed Actions:** No adding/editing/removing items. The cart's contents are frozen.
    *   **Transition:** `Open` -> `Locked` (Triggered by Host).

3.  **`ReadyToConfirm`:** The "ready for conversion" state.
    *   **Allowed Actions:** None, except for the Host to trigger the final conversion.
    *   **Transition:** `Locked` -> `ReadyToConfirm` (Triggered automatically when the last member pays).

4.  **`Converted` / `Expired`:** Terminal states.

---

### Phase 1: Domain Layer Refactoring

#### **1.1. Refactor the `TeamCart` Aggregate**

The `TeamCart` aggregate itself needs minimal structural changes but requires internal updates to its financial logic.

*   **Objective:** Align internal calculations with the new centralized domain service.
*   **Methods to Refactor:**
    *   **`ApplyCoupon(...)`:** This method's implementation will be changed. Instead of calculating the discount itself, it will now call the `OrderFinancialService`.
        *   **Old Logic:** Contained complex calculation logic.
        *   **New Logic:**
            1.  Performs its own invariant checks (requestor is host, status is valid, etc.).
            2.  Calls `_orderFinancialService.ValidateAndCalculateDiscount(...)`, passing its own items and subtotal.
            3.  If the result is successful, it updates its own `DiscountAmount` and `AppliedCouponId` properties.
*   **Methods to Keep:**
    *   `CommitToCashOnDelivery(...)` and `RecordSuccessfulOnlinePayment(...)` are **correct and essential**. They are the mechanisms that allow the `TeamCart` to reach the `ReadyToConfirm` state. They will remain unchanged.

*   **Objective:** Implement the new `Open` -> `Locked` -> `ReadyToConfirm` lifecycle.
*   **`TeamCartStatus` Enum:** `Open`, `Locked`, `ReadyToConfirm`, `Converted`, `Expired`.
*   **Public Methods to Add/Modify:**
    *   **`public Result LockForPayment(UserId requestingUserId)`:** (Replaces `InitiateCheckout`)
        *   **Purpose:** The Host's action to end the item-adding phase and begin the payment phase.
        *   **Logic:**
            1.  Checks if `requestingUserId` is the Host.
            2.  Checks if `Status == Open`.
            3.  Checks if there are items in the cart.
            4.  If all checks pass, transitions `Status` to `Locked`.
            5.  Raises a `TeamCartLockedForPayment` event. This event is crucial for notifying all members via the frontend that it's time to pay.
    *   **`AddItem(...)`:**
        *   Adds an invariant check: `if (Status != TeamCartStatus.Open) return TeamCartErrors.CannotAddItemsToLockedCart;`
    *   **`ApplyTip(...)`, `ApplyCoupon(...)`:**
        *   Adds an invariant check: `if (Status != TeamCartStatus.Locked) return TeamCartErrors.CanOnlyApplyFinancialsToLockedCart;`
    *   **`CommitToCashOnDelivery(...)`, `RecordSuccessfulOnlinePayment(...)`:**
        *   Adds an invariant check: `if (Status != TeamCartStatus.Locked) return TeamCartErrors.CanOnlyPayOnLockedCart;`
    *   **Internal Logic for `ReadyToConfirm`:** The automatic transition to `ReadyToConfirm` after the last payment is made remains the same, but it now happens from the `Locked` state.

#### **1.2. Refactor the `TeamCartConversionService`**

This is where the most significant changes occur. The service must be updated to call the new `Order.Create` factory method with a complete and consistent set of parameters.

*   **Objective:** Rewrite the `ConvertToOrder` method to correctly map a `ReadyToConfirm` `TeamCart` to a `Placed` `Order`.
*   **File: `src/Domain/Services/TeamCartConversionService.cs` (Refactored)**
    ```csharp

    /// <summary>
    /// Domain service responsible for converting a finalized TeamCart to a Placed Order.
    /// It uses OrderFinancialService for consistent calculations.
    /// </summary>
    public sealed class TeamCartConversionService
    {
        private readonly OrderFinancialService _financialService;

        // The service now depends on the financial service for calculations.
        public TeamCartConversionService(OrderFinancialService financialService)
        {
            _financialService = financialService;
        }

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

            // 2. Map TeamCartItems to OrderItems
            var orderItems = MapToOrderItems(teamCart.Items);
            if (!orderItems.Any())
            {
                return Result.Failure<(Order, TeamCart)>(TeamCartErrors.CannotConvertWithoutItems);
            }

            // 3. Use the financial service to get consistent calculations
            var subtotal = _financialService.CalculateSubtotal(orderItems);
            var tip = teamCart.TipAmount;
            var discount = teamCart.DiscountAmount;
            // For now, assume delivery fee and tax are zero for team carts
            var deliveryFee = Money.Zero(subtotal.Currency);
            var tax = Money.Zero(subtotal.Currency);

            var totalAmount = _financialService.CalculateFinalTotal(subtotal, discount, deliveryFee, tip, tax);

            // 4. Map MemberPayments to pre-confirmed PaymentTransactions
            var paymentTransactions = MapToPaymentTransactions(teamCart, totalAmount);
            if (paymentTransactions.IsFailure)
            {
                return Result.Failure<(Order, TeamCart)>(paymentTransactions.Error);
            }

            // 5. Create the Order using the refactored factory method
            // The Order is created directly as 'Placed' because all payments are pre-confirmed.
            var orderResult = Order.Create(
                teamCart.HostUserId,
                teamCart.RestaurantId,
                deliveryAddress,
                orderItems,
                specialInstructions,
                subtotal,
                discount,
                deliveryFee,
                tip,
                tax,
                totalAmount,
                paymentTransactions.Value, // Pass the successful list
                teamCart.AppliedCouponId,
                // --- Key differences for TeamCart conversion ---
                initialStatus: OrderStatus.Placed, // It's already paid/committed
                paymentIntentId: null, // No single payment intent for the whole order
                sourceTeamCartId: teamCart.Id);

            if (orderResult.IsFailure)
            {
                // This would indicate a bug, as all data should be consistent.
                return Result.Failure<(Order, TeamCart)>(orderResult.Error);
            }

            // 6. Mark the TeamCart as converted
            var conversionResult = teamCart.MarkAsConverted();
            if (conversionResult.IsFailure)
            {
                return Result.Failure<(Order, TeamCart)>(conversionResult.Error);
            }

            return (orderResult.Value, teamCart);
        }

        private List<OrderItem> MapToOrderItems(IReadOnlyList<TeamCartItem> cartItems)
        {
            // This logic can be extracted but is shown here for clarity.
            // It maps TeamCartItem to OrderItem, including customizations.
            // (Implementation would be similar to the original file)
            // ... returns List<OrderItem>
        }

        private Result<List<PaymentTransaction>> MapToPaymentTransactions(TeamCart teamCart, Money finalTotalAmount)
        {
            var transactions = new List<PaymentTransaction>();
            var totalPaidByMembers = teamCart.MemberPayments.Sum(p => p.Amount.Amount);

            // This factor distributes the final tip/discount proportionally across all payments.
            var adjustmentFactor = totalPaidByMembers > 0 ? finalTotalAmount.Amount / totalPaidByMembers : 1;

            foreach (var payment in teamCart.MemberPayments)
            {
                var adjustedAmount = new Money(Math.Round(payment.Amount.Amount * adjustmentFactor, 2), payment.Amount.Currency);
                PaymentTransaction transaction;

                if (payment.Method == PaymentMethod.Online && payment.Status == TeamCartAggregate.Enums.PaymentStatus.PaidOnline)
                {
                    var txResult = PaymentTransaction.CreateSucceededOnline(
                        adjustedAmount,
                        payment.OnlineTransactionId!,
                        payment.UserId);
                    if (txResult.IsFailure) return Result.Failure<List<PaymentTransaction>>(txResult.Error);
                    transaction = txResult.Value;
                }
                else if (payment.Method == PaymentMethod.CashOnDelivery && payment.Status == TeamCartAggregate.Enums.PaymentStatus.CommittedToCOD)
                {
                    // Group all COD payments later
                    continue;
                }
                else
                {
                    // Should not happen if cart is ReadyToConfirm
                    return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.ConversionDataIncomplete);
                }
                transactions.Add(transaction);
            }
            
            // Aggregate all COD payments into a single transaction guaranteed by the host
            var codPayments = teamCart.MemberPayments.Where(p => p.Method == PaymentMethod.CashOnDelivery).ToList();
            if (codPayments.Any())
            {
                var totalCodAmount = codPayments.Sum(p => p.Amount.Amount);
                var adjustedCodAmount = new Money(Math.Round(totalCodAmount * adjustmentFactor, 2), finalTotalAmount.Currency);
                
                var codTxResult = PaymentTransaction.CreatePendingCod(adjustedCodAmount, teamCart.HostUserId);
                if (codTxResult.IsFailure) return Result.Failure<List<PaymentTransaction>>(codTxResult.Error);
                transactions.Add(codTxResult.Value);
            }
            
            // Final check to ensure the sum of new transactions matches the order total
            var sumOfNewTransactions = transactions.Sum(t => t.Amount.Amount);
            if (Math.Abs(sumOfNewTransactions - finalTotalAmount.Amount) > 0.01m) // Use a small tolerance for rounding
            {
                return Result.Failure<List<PaymentTransaction>>(OrderErrors.FinancialMismatch);
            }

            return transactions;
        }
    }
    ```

---

### Phase 2: Application Layer Refactoring

The command handler for converting a `TeamCart` becomes much simpler.

*   **Command:** `ConvertTeamCartToOrderCommand(TeamCartId, HostUserId, DeliveryAddress, SpecialInstructions)`
*   **Handler Orchestration:**
    1.  **Start Transaction & Fetch Data:**
        *   Begin `IUnitOfWork` transaction.
        *   Fetch the `TeamCart` aggregate using `_teamCartRepository.GetByIdAsync(command.TeamCartId)`.
        *   Validate that `command.HostUserId` is indeed the `teamCart.HostUserId`.
    2.  **Invoke Domain Service:**
        *   Instantiate the `TeamCartConversionService` (passing in the `OrderFinancialService`).
        *   `var conversionResult = _conversionService.ConvertToOrder(teamCart, command.DeliveryAddress, command.SpecialInstructions);`
        *   If `conversionResult.IsFailure`, return the error.
    3.  **Persist Aggregates:**
        *   `var (newOrder, updatedTeamCart) = conversionResult.Value;`
        *   `await _orderRepository.AddAsync(newOrder);`
        *   `await _teamCartRepository.UpdateAsync(updatedTeamCart);`
    4.  **Commit Transaction:**
        *   Complete the `IUnitOfWork`. This saves both aggregates and dispatches all domain events (`OrderCreated`, `TeamCartConverted`, etc.).
    5.  **Return Response:** Return the `newOrder.Id`.

This is where the new flow comes to life.

1.  **Shopping Phase (`Open` Status):**
    *   The UI allows members to freely add items. The "Pay" button for each member is **disabled**.
    *   The Host sees a prominent button: **"Lock Cart & Proceed to Payment"**.

2.  **Locking Action:**
    *   The Host clicks the button, triggering the `LockTeamCartForPaymentCommand`.
    *   The backend executes the `teamCart.LockForPayment()` method.
    *   The `TeamCartLockedForPayment` event is dispatched.

3.  **Payment Phase (`Locked` Status):**
    *   The frontend, listening for real-time updates (e.g., via WebSockets), receives the notification that the cart is now `Locked`.
    *   **UI Changes for All Members:**
        *   The item list becomes read-only.
        *   The "Pay" button for each member becomes **enabled**.
        *   A section appears showing the shared tip and discount, which the Host can now modify.
    *   As the Host adjusts the tip/coupon, all members see their final totals update in real-time.
    *   Members can now proceed to pay (COD or Online) at their leisure.

4.  **Finalization Phase (`ReadyToConfirm` Status):**
    *   When the last member pays, the cart automatically transitions to `ReadyToConfirm`.
    *   The `TeamCartReadyForConfirmation` event is dispatched.
    *   **UI Change for Host:** The "Place Group Order" button becomes enabled.

5.  **Conversion:**
    *   The Host clicks the final button, triggering the `ConvertTeamCartToOrderCommand`. The rest of the flow proceeds as designed previously.

### Summary of Benefits of the Hybrid Model

*   **Financial Integrity (Solves the Core Problem):** All financial calculations are performed on a fixed set of items. Tips and coupons are applied *before* any payments are collected, ensuring every member pays the correct, final amount.
*   **Clear User Workflow:** The separation of "shopping" and "paying" is explicit and easy for users to understand. It prevents confusion.
*   **Improved Host Control:** The Host has a clear, deliberate action to finalize the order details before asking for money.
*   **Retains Flexibility:** While it introduces a locking point, the payment phase itself is still flexible. Members are not forced to pay at the exact same moment; they are simply notified that the payment window is open.
*   **Technically Sound:** This model is robust and avoids the race conditions and reconciliation issues of the "continuous add & pay" model.

This hybrid approach correctly balances user experience with the transactional necessities of a group payment system. It is the most robust and correct design.