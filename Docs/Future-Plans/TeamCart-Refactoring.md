
### Conceptual Model: The "Lock, Settle, Convert" Lifecycle

Your `Open` -> `Locked` -> `ReadyToConfirm` model is perfect. Let's call it "Lock, Settle, Convert" to emphasize the actions.

1. **Open:** The "Wild West" phase. Members join, add/remove items. The cart is fluid.
2. **Locked:** The Host locks the cart. Item-related changes are now forbidden. This is the "Settle Up" phase. The Host can now apply final financials (tip, coupon). Members are notified to pay their share (either commit to COD or pay online).
3. **ReadyToConfirm:** All members have settled their dues. The cart is now immutable and ready for the final step.
4. **Converted:** The `TeamCartConversionService` has successfully created an `Order` from the `TeamCart`. This is a terminal state.
5. **Expired:** The cart timed out before conversion. This is also a terminal state.

This model provides clear, enforceable invariants at each stage, which is the cornerstone of a strong aggregate design.

---

### The Refactoring Plan: Step-by-Step

Here is a detailed plan to implement this new lifecycle.

#### Step 1: Refactor the `TeamCart` Aggregate

The goal is to make the aggregate enforce the new lifecycle rules internally.

**1.1. Update `TeamCartStatus` Enum:**
As you suggested, replace `AwaitingPayments` with `Locked`.

```csharp
// src\Domain\TeamCartAggregate\Enums\TeamCartStatus.cs
public enum TeamCartStatus
{
    Open,
    Locked, // Replaces AwaitingPayments
    ReadyToConfirm,
    Converted,
    Expired
}
```

Add XML documentation to clarify the purpose of each status.

**1.2. Replace `InitiateCheckout` with `LockForPayment`:**
This method is the pivotal state transition from `Open` to `Locked`.

```csharp
// In TeamCart.cs
// REMOVE: public Result InitiateCheckout(UserId requestingUserId)

// ADD:
/// <summary>
/// Locks the team cart, preventing further item modifications and initiating the payment phase.
/// Only the host can perform this action.
/// </summary>
public Result LockForPayment(UserId requestingUserId)
{
    if (requestingUserId != HostUserId)
    {
        return Result.Failure(TeamCartErrors.OnlyHostCanLockCart); // New Error
    }

    if (Status != TeamCartStatus.Open)
    {
        return Result.Failure(TeamCartErrors.CannotLockCartInCurrentStatus); // New Error
    }

    if (!_items.Any())
    {
        return Result.Failure(TeamCartErrors.CannotLockEmptyCart); // New Error
    }

    Status = TeamCartStatus.Locked;
    AddDomainEvent(new TeamCartLockedForPayment(Id, HostUserId)); // New Event
    return Result.Success();
}
```

**1.3. Update Invariants in Public Methods:**
Enforce the rules of the new lifecycle.

* **Item/Member Management:**

    ```csharp
    // In TeamCart.AddItem, AddMember, etc.
    if (Status != TeamCartStatus.Open)
    {
        return Result.Failure(TeamCartErrors.CannotModifyCartOnceLocked); // New Error
    }
    ```

* **Financials (Tip/Coupon):** These can *only* be applied when the cart is locked, as the subtotal is now stable.

    ```csharp
    // In TeamCart.ApplyTip, ApplyCoupon, RemoveCoupon
    if (Status != TeamCartStatus.Locked)
    {
        return Result.Failure(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart); // New Error
    }
    ```

* **Payments:** Members can only pay once the cart is locked.

    ```csharp
    // In TeamCart.CommitToCashOnDelivery, RecordSuccessfulOnlinePayment
    if (Status != TeamCartStatus.Locked)
    {
        return Result.Failure(TeamCartErrors.CanOnlyPayOnLockedCart); // New Error
    }
    ```

**1.4. Refactor Coupon Application Logic:**
The `TeamCart` should only record the *intent* to use a coupon, not perform the calculation. The calculation is deferred to the conversion service.

```csharp
// In TeamCart.cs
public class TeamCart : ...
{
    // ...
    // This property remains, but it's now calculated and set ONLY during conversion.
    // Let's reset it when a coupon is applied/removed.
    public Money DiscountAmount { get; private set; } 
    // ...

    // REMOVE the old ApplyCoupon method.
    // public Result ApplyCoupon(UserId requestingUserId, CouponId couponId, CouponValue couponValue, ...)

    // ADD the new, simpler method:
    /// <summary>
    /// Applies a coupon to the team cart by storing its ID. The actual discount
    /// is calculated upon conversion to an order.
    /// </summary>
    public Result ApplyCoupon(UserId requestingUserId, CouponId couponId)
    {
        if (requestingUserId != HostUserId)
        {
            return Result.Failure(TeamCartErrors.OnlyHostCanModifyFinancials);
        }

        if (Status != TeamCartStatus.Locked)
        {
            return Result.Failure(TeamCartErrors.CanOnlyApplyFinancialsToLockedCart);
        }

        if (AppliedCouponId is not null)
        {
            return Result.Failure(TeamCartErrors.CouponAlreadyApplied);
        }

        AppliedCouponId = couponId;
        // Reset the discount amount. It's now invalid until recalculated at conversion.
        DiscountAmount = Money.Zero(DiscountAmount.Currency); 
        return Result.Success();
    }

    public Result RemoveCoupon(UserId requestingUserId)
    {
        // ... guard clauses ...
        AppliedCouponId = null;
        // Reset the discount amount.
        DiscountAmount = Money.Zero(DiscountAmount.Currency);
        return Result.Success();
    }

    // ADD a new internal method to be used by the conversion service.
    /// <summary>
    /// Internally sets the final calculated discount amount. Should only be called
    /// by a trusted domain service during the order conversion process.
    /// </summary>
    internal void SetFinalDiscount(Money discount)
    {
        DiscountAmount = discount;
    }
}
```

**1.5. Update `CheckAndTransitionToReadyToConfirm`:**
The internal logic is the same, but it's triggered from the `Locked` state.

```csharp
// In TeamCart.cs
private void CheckAndTransitionToReadyToConfirm()
{
    if (Status != TeamCartStatus.Locked) // The only change needed here
    {
        return;
    }

    // ... rest of the logic is the same ...
}
```

---

#### Step 2: Refactor `TeamCartConversionService`

This service becomes a pure orchestrator, delegating all complex logic to other domain services and aggregates.

**2.1. Inject `OrderFinancialService`:**

```csharp
// src\Domain\Services\TeamCartConversionService.cs
public sealed class TeamCartConversionService
{
    private readonly OrderFinancialService _financialService;

    public TeamCartConversionService(OrderFinancialService financialService)
    {
        _financialService = financialService;
    }
    // ...
}
```

*(Note: You'll need to register `OrderFinancialService` in your DI container as transient or scoped).*

**2.2. Refactor `ConvertToOrder` Method:**
The method will now take the `Coupon` object (if any) as an argument, which the Application Layer handler is responsible for fetching.

```csharp
// src\Domain\Services\TeamCartConversionService.cs
public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(
    TeamCart teamCart,
    DeliveryAddress deliveryAddress,
    string specialInstructions,
    Coupon? coupon, // The full Coupon object, if one was applied.
    int currentUserCouponUsageCount, // For validation
    Money deliveryFee, // These should come from other services/config
    Money taxAmount)   // in a real app.
{
    // 1. Validate State
    if (teamCart.Status != TeamCartStatus.ReadyToConfirm)
    {
        return Result.Failure<(Order, TeamCart)>(TeamCartErrors.InvalidStatusForConversion);
    }
    
    // 2. Map TeamCartItems to OrderItems (This logic is fine, no changes needed)
    var orderItems = MapToOrderItems(teamCart.Items);

    // 3. Perform All Financial Calculations using OrderFinancialService
    // Remove all private calculation helpers like CalculateSubtotal, CalculateTotalAmount.
    var subtotal = _financialService.CalculateSubtotal(orderItems);

    Money discountAmount = Money.Zero(subtotal.Currency);
    if (coupon is not null && teamCart.AppliedCouponId == coupon.Id)
    {
        var discountResult = _financialService.ValidateAndCalculateDiscount(
            coupon,
            currentUserCouponUsageCount,
            orderItems,
            subtotal);

        if (discountResult.IsFailure)
        {
            return Result.Failure<(Order, TeamCart)>(discountResult.Error);
        }
        discountAmount = discountResult.Value;
        
        // IMPORTANT: Update the TeamCart with the final calculated discount for historical accuracy.
        teamCart.SetFinalDiscount(discountAmount);
    }

    var totalAmount = _financialService.CalculateFinalTotal(
        subtotal, 
        discountAmount, 
        deliveryFee, 
        teamCart.TipAmount, 
        taxAmount);

    // 4. Create Succeeded PaymentTransactions for the Order
    // The key insight: Since TeamCart is ReadyToConfirm, all its payments have already succeeded.
    // We are creating a historical record for the new Order.
    var paymentTransactions = CreateSucceededPaymentTransactions(teamCart, totalAmount, subtotal);
    if (paymentTransactions.IsFailure)
    {
        return Result.Failure<(Order, TeamCart)>(paymentTransactions.Error);
    }

    // 5. Create the Order
    // The Order will be created directly in the 'Placed' status because all payments are confirmed.
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
        paymentTransactions.Value, // Pass the list of succeeded transactions
        teamCart.AppliedCouponId,
        OrderStatus.Placed, // Directly to Placed, skipping AwaitingPayment
        paymentIntentId: null, // Not needed, we have individual transaction references
        sourceTeamCartId: teamCart.Id);

    if (orderResult.IsFailure)
    {
        return Result.Failure<(Order, TeamCart)>(orderResult.Error);
    }

    // 6. Finalize TeamCart State
    var conversionResult = teamCart.MarkAsConverted();
    if (conversionResult.IsFailure)
    {
        // This should theoretically never happen if the status check at the top passed.
        return Result.Failure<(Order, TeamCart)>(conversionResult.Error);
    }

    var order = orderResult.Value;
    teamCart.AddDomainEvent(new TeamCartConverted(teamCart.Id, order.Id, DateTime.UtcNow, teamCart.HostUserId));

    return (order, teamCart);
}

// REMOVE: CreatePaymentTransactionsFrom, CalculateSubtotal, CalculateTotalAmount

// ADD a new, more robust helper for creating transactions
private Result<List<PaymentTransaction>> CreateSucceededPaymentTransactions(TeamCart teamCart, Money totalAmount, Money subtotal)
{
    var transactions = new List<PaymentTransaction>();
    var totalPaidByMembers = teamCart.MemberPayments.Sum(p => p.Amount.Amount);
    
    // This factor accounts for the host-added tip and coupon discount, distributing it proportionally.
    var adjustmentFactor = totalPaidByMembers > 0 ? totalAmount.Amount / totalPaidByMembers : 1;

    foreach (var memberPayment in teamCart.MemberPayments)
    {
        var adjustedAmount = new Money(memberPayment.Amount.Amount * adjustmentFactor, memberPayment.Amount.Currency);
        var paymentMethodType = memberPayment.Method == PaymentMethod.Online 
            ? PaymentMethodType.CreditCard // Or derive from more specific data if available
            : PaymentMethodType.CashOnDelivery;

        var transactionResult = PaymentTransaction.Create(
            paymentMethodType,
            PaymentTransactionType.Payment,
            adjustedAmount,
            DateTime.UtcNow,
            paymentGatewayReferenceId: memberPayment.OnlineTransactionId,
            paidByUserId: memberPayment.UserId);
            
        if(transactionResult.IsFailure) return Result.Failure<List<PaymentTransaction>>(transactionResult.Error);

        var transaction = transactionResult.Value;
        transaction.MarkAsSucceeded(); // Mark as succeeded immediately
        transactions.Add(transaction);
    }
    
    // Final sanity check
    var finalTransactionSum = transactions.Sum(t => t.Amount.Amount);
    if (Math.Abs(finalTransactionSum - totalAmount.Amount) > 0.01m) // Use a tolerance for rounding
    {
        return Result.Failure<List<PaymentTransaction>>(TeamCartErrors.FinalPaymentMismatch); // New Error
    }

    return transactions;
}

private List<OrderItem> MapToOrderItems(IReadOnlyList<TeamCartItem> cartItems)
{
    // This mapping logic can be extracted from your old service.
    // It's mostly correct, just ensure it returns List<OrderItem>.
    // ... implementation ...
    return new List<OrderItem>(); // Placeholder
}
```

---

### Step 3: Update Application Layer (Example Handler)

The application layer's responsibility is to fetch data and orchestrate the domain services.

```csharp
public class ConvertTeamCartToOrderCommandHandler // Example
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly TeamCartConversionService _conversionService;
    private readonly IUnitOfWork _unitOfWork;

    // ... constructor ...

    public async Task<Result<OrderDto>> Handle(ConvertTeamCartToOrderCommand command)
    {
        // 1. Fetch Aggregates
        var teamCart = await _teamCartRepository.GetByIdAsync(command.TeamCartId);
        if (teamCart is null) return Result.Failure<OrderDto>(...);

        // Check host authorization
        if (teamCart.HostUserId != command.RequestingUserId) ...

        Coupon? coupon = null;
        int userCouponUsage = 0;
        if (teamCart.AppliedCouponId is not null)
        {
            coupon = await _couponRepository.GetByIdAsync(teamCart.AppliedCouponId);
            userCouponUsage = await _couponRepository.GetUserUsageCountAsync(coupon.Id, teamCart.HostUserId);
        }

        // 2. Get other required data (e.g., delivery fees, tax)
        var deliveryFee = Money.Zero("USD"); // from a delivery service
        var taxAmount = Money.Zero("USD");   // from a tax service

        // 3. Call the Domain Service to do the work
        var conversionResult = _conversionService.ConvertToOrder(
            teamCart,
            command.DeliveryAddress,
            command.SpecialInstructions,
            coupon,
            userCouponUsage,
            deliveryFee,
            taxAmount);

        if (conversionResult.IsFailure)
        {
            return Result.Failure<OrderDto>(conversionResult.Error);
        }

        // 4. Persist Changes
        var (order, updatedTeamCart) = conversionResult.Value;
        await _orderRepository.AddAsync(order);
        await _teamCartRepository.UpdateAsync(updatedTeamCart);
        await _unitOfWork.SaveChangesAsync(); // This also dispatches domain events

        // 5. Return DTO
        return Result.Success(new OrderDto(order.Id));
    }
}
```

### Final touches

Let's trace the flow with `DiscountAmount` removed from `TeamCart`.

1.  **`TeamCart` Aggregate:**
    *   Still has `public CouponId? AppliedCouponId { get; private set; }`.
    *   The `ApplyCoupon(UserId, CouponId)` method simply sets this ID.
    *   The `DiscountAmount` property is **completely removed**.
    *   The `internal SetFinalDiscount()` method is **completely removed**.

2.  **`TeamCartConversionService.ConvertToOrder` Method:**
    *   The signature remains the same, taking the `Coupon` object.
    *   It calls `_financialService.ValidateAndCalculateDiscount(...)` to get the `discountAmount`.
    *   It calls `_financialService.CalculateFinalTotal(...)`, passing in the `discountAmount` it just calculated.
    *   It calls `Order.Create(...)`, passing the `discountAmount` **directly** to the `Order`'s factory method.
    *   **Crucially, it never tries to set this value back onto the `TeamCart` object.**

**Code Snippet of the Refined `ConvertToOrder`:**

```csharp

// In TeamCartConversionService.cs
public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(...)
{

    // ... (Validate state, map items) ...

    var subtotal = _financialService.CalculateSubtotal(orderItems);

    // Calculate the discount. This value is now a local variable within this service method.

    Money discountAmount = Money.Zero(subtotal.Currency);

    if (coupon is not null && teamCart.AppliedCouponId == coupon.Id)

    {

        var discountResult = _financialService.ValidateAndCalculateDiscount(...);

        if (discountResult.IsFailure) { /* ... */ }

        discountAmount = discountResult.Value; // Stored locally, not on the TeamCart.

    }

    var totalAmount = _financialService.CalculateFinalTotal(subtotal, discountAmount, ...);

    var paymentTransactions = CreateSucceededPaymentTransactions(teamCart, totalAmount, subtotal);

    // ...

    // The calculated discountAmount is passed DIRECTLY to the Order.

    var orderResult = Order.Create(

        // ...,

        discountAmount: discountAmount,

        // ...,
        totalAmount: totalAmount,
        // ...
    );
    if (orderResult.IsFailure) { /* ... */ }
    // No call to teamCart.SetFinalDiscount() is needed.
    var conversionResult = teamCart.MarkAsConverted();
    // ...
    return (orderResult.Value, teamCart);
}
```

### Summary of Benefits

This refactoring aligns the entire `TeamCart`-to-`Order` flow with your new, robust domain model:

1. **Clearer Aggregate Lifecycle:** The `TeamCart` states (`Open`, `Locked`, `ReadyToConfirm`) are unambiguous and enforce correct behavior at each stage.
2. **Single Responsibility Principle (SRP):**
    * `TeamCart` manages its own state and entities.
    * `OrderFinancialService` handles all complex financial logic.
    * `TeamCartConversionService` purely orchestrates the conversion process.
3. **Consistency:** The creation of the final `Order` perfectly matches the rules of the `Order` aggregate, creating a `Placed` order with a complete, historical list of `Succeeded` payment transactions.
4. **Reduced Duplication:** Financial calculation logic is centralized in `OrderFinancialService`, no longer duplicated in `TeamCartConversionService`.
5. **Enhanced Testability:** Each component (`TeamCart`, `OrderFinancialService`, `TeamCartConversionService`) can be unit-tested in isolation with clear responsibilities.

---

Let's follow a practical example: **Alex, the team lead, wants to order pizza for his team members, Brenda and Chris.**

---

### The TeamCart Lifecycle: A Pizza Order Story

#### Phase 1: Creation & Operation (Status: `Open`)

This is the collaborative, "anything goes" phase.

1. **Alex Creates the Cart:** Alex navigates to "Start a Team Order" on YummyZoom. He selects the pizza restaurant.
    * **User Action:** Clicks "Create Team Cart".
    * **System Logic:**
        * The application calls `TeamCart.Create(hostUserId: alex.Id, ...)`.
        * A new `TeamCart` aggregate is created.
        * Its **`Status` is set to `Open`**.
        * A unique shareable link (`ShareToken`) is generated.
        * Alex is automatically added as the first member with the `Host` role.

2. **Members Join and Add Items:** Alex shares the link with Brenda and Chris.
    * **User Action:** Brenda and Chris click the link, enter their names, and join the cart. They then browse the menu and add their desired pizzas and drinks.
    * **System Logic:**
        * For each join, `TeamCart.AddMember()` is called. The `_members` list grows.
        * For each item added, `TeamCart.AddItem()` is called. The `_items` list grows.
        * **Invariant Check:** The system continuously checks `if (Status == TeamCartStatus.Open)` before allowing any of these modifications.

At this point, the `TeamCart` is `Open`. Members can join, leave, add items, or change their items. No payments can be made, and no final tip or coupon can be applied because the subtotal is still fluctuating.

---

#### Phase 2: Locking & Settling (Status: `Locked`)

The Host decides the order is complete and initiates the payment process.

1. **Alex Locks the Cart:** Everyone has added their items. Alex is ready to move forward.
    * **User Action:** Alex clicks the "Lock & Proceed to Payment" button.
    * **System Logic:**
        * The application calls `teamCart.LockForPayment(requestingUserId: alex.Id)`.
        * The aggregate verifies that Alex is the `Host` and the status is `Open`.
        * The **`Status` transitions from `Open` to `Locked`**.
        * A `TeamCartLockedForPayment` domain event is raised. This event is used to send real-time notifications (e.g., via WebSockets) to Brenda and Chris's screens: "The cart is locked! Please pay for your items."

2. **Alex Finalizes Financials:** Now that the items are locked and the subtotal is fixed, Alex can add the final touches.
    * **User Action:** Alex adds a $5 tip and applies a "20% OFF" coupon code he has.
    * **System Logic:**
        * `teamCart.ApplyTip(...)` is called. The `TipAmount` property is set.
        * `teamCart.ApplyCoupon(...)` is called. The `AppliedCouponId` is stored. The actual discount value is **not** calculated yet.
        * **Invariant Check:** These methods now succeed because the `Status` is `Locked`.

3. **Members Settle Their Dues:** Brenda and Chris see their final amounts and proceed to pay.
    * **User Action (Brenda):** Brenda chooses to pay with her credit card. The UI integrates with Stripe, she enters her details, and the payment succeeds.
    * **System Logic (Brenda):** A webhook from Stripe confirms the payment. The application calls `teamCart.RecordSuccessfulOnlinePayment(userId: brenda.Id, ..., transactionId: "pi_...")`. A `MemberPayment` record is added for Brenda with `Status = PaidOnline`.
    * **User Action (Chris):** Chris prefers to pay with cash. He clicks "Commit to Cash on Delivery".
    * **System Logic (Chris):** The application calls `teamCart.CommitToCashOnDelivery(userId: chris.Id, ...)`. A `MemberPayment` record is added for Chris with `Status = CommittedToCOD`.

The `TeamCart` is now `Locked`. No one can change their items. Members can only perform payment actions.

---

#### Phase 3: Ready for Conversion (Status: `ReadyToConfirm`)

This is an **automatic transition** that happens when the last member pays.

1. **The Final Payment Triggers the Transition:** Let's say Chris committed to COD first. The cart remained `Locked`. Then, Brenda's successful online payment was the last one needed.
    * **User Action:** None. This is a system reaction.
    * **System Logic:**
        * Inside the `teamCart.RecordSuccessfulOnlinePayment(...)` method for Brenda, after adding her payment, the private helper `CheckAndTransitionToReadyToConfirm()` is called.
        * This helper method checks:
            1. Is the status `Locked`? **Yes.**
            2. Has every member paid or committed to pay? **Yes.** (Assuming Alex's items are also covered by a payment).
        * Since all conditions are met, the **`Status` transitions from `Locked` to `ReadyToConfirm`**.
        * A `TeamCartReadyForConfirmation` domain event is raised.

The UI for Alex now updates, enabling a "Confirm and Place Order" button. The cart is fully settled and immutable, waiting for the final green light.

---

#### Phase 4: Conversion to Order (Status: `Converted`)

This is the final, irreversible step where the `TeamCart` becomes a real `Order`.

1. **Alex Places the Order:**
    * **User Action:** Alex reviews the complete order and clicks "Confirm and Place Order".
    * **System Logic:**
        1. The application calls the `TeamCartConversionService`.
        2. The service validates the cart is `ReadyToConfirm`.
        3. It calls the `OrderFinancialService` to calculate the final, official `discountAmount` (using the stored coupon ID) and the final `totalAmount`.
        4. It creates a list of **new `PaymentTransaction` entities** for the `Order`. Brenda's becomes a `Succeeded` online transaction, and Chris's becomes a `Succeeded` COD transaction.
        5. It calls `Order.Create(...)`, passing in all the calculated financials and the list of succeeded transactions. The new `Order` is created instantly with **`OrderStatus.Placed`**.
        6. Finally, it calls `teamCart.MarkAsConverted()`. The **`TeamCart` status transitions to `Converted`**.
        7. The new `Order` and the updated `TeamCart` are saved to the database.

The `TeamCart` has fulfilled its purpose. It's now an archived, historical record. A new `Order` aggregate has taken over and will proceed through its own lifecycle (`Placed` -> `Accepted` -> `Preparing` -> `Delivered`). Alex, Brenda, and Chris all receive an "Order Placed!" confirmation.
