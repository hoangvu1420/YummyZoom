### Final Outline for Refactoring the Order Creation Process

This refactoring redesigns the order creation workflow to be safer, more robust, and more aligned with DDD principles. It clearly separates responsibilities between the Application Layer, stateless Domain Services, and a lean `Order` aggregate.

#### **Phase 1: Domain Layer Refactoring**

**1.1. Refactor the `Order` Aggregate (`src/Domain/OrderAggregate/Order.cs`)**

* **Core Principle:** The `Order` aggregate is an immutable record of a confirmed transaction. It does not perform calculations; it validates the consistency of a completed process.
* **Public Methods to Remove:** The business logic from these methods is moved to stateless domain services.
  * `ApplyCoupon()`
  * `RemoveCoupon()`
  * `AddPaymentAttempt()`
  * `MarkAsPaid()`
* **`Create` Factory Method Refactoring:**
  * **Signature:**

        ```csharp
        public static Result<Order> Create(
            UserId customerId,
            RestaurantId restaurantId,
            DeliveryAddress deliveryAddress,
            List<OrderItem> orderItems,
            string specialInstructions,
            // --- Required, Pre-Calculated Financial Details ---
            Money subtotal,
            Money discountAmount,
            Money deliveryFee,
            Money tipAmount,
            Money taxAmount,
            Money totalAmount, // The final, expected total
            // --- Required Transaction & Reference Details ---
            List<PaymentTransaction> paymentTransactions,
            CouponId? appliedCouponId,
            TeamCartId? sourceTeamCartId = null)
        ```

  * **Internal Logic:** The factory method acts as a final consistency gatekeeper.
        1. It calculates a transient total based on the provided `subtotal`, `discountAmount`, and other fees.
        2. **Invariant Check 1 (Financial Integrity):** Asserts that its internally calculated total equals the `totalAmount` parameter. If not, `return OrderErrors.FinancialMismatch`.
        3. **Invariant Check 2 (Payment Integrity):** Calculates the sum of all `paymentTransactions` amounts.
        4. **Invariant Check 3 (Payment Match):** Asserts that the sum of payments equals the `totalAmount`. If not, `return OrderErrors.PaymentMismatch`.
        5. If all checks pass, it creates and returns the new `Order` instance, raising an `OrderCreated` domain event.

**1.2. Implement Concrete, Stateless Domain Services (`src/Domain/Services/`)**

* **Core Principle:** These services are pure, stateless calculators. They live in the Domain Layer but have no dependencies on infrastructure (no I/O, no repositories).
* **File: `src/Domain/Services/OrderFinancialService.cs`**
  * **Purpose:** A single service for all order-related financial logic.
  * **Methods:**
    * `public Money CalculateSubtotal(IReadOnlyList<OrderItem> orderItems)`: Sums the `LineItemTotal` of all items.
    * `public Result<Money> ValidateAndCalculateDiscount(Coupon coupon, int currentUserUsageCount, IReadOnlyList<OrderItem> orderItems, Money subtotal)`:
      * Performs all business rule checks for a coupon: active status, validity dates, usage limits (both total and per-user), and minimum order amount.
      * Calculates the discount value based on the coupon's type (`Percentage`, `FixedAmount`, `FreeItem`) and scope (`WholeOrder`, `SpecificItems`, etc.).
      * Returns a `Result` object containing the calculated discount `Money` or a specific `CouponError`.
    * `public Money CalculateFinalTotal(Money subtotal, Money discount, Money deliveryFee, Money tip, Money tax)`: Calculates the final charge amount, ensuring it is not negative.
* **Implementation:**

    ```csharp
    public class OrderFinancialService
    {
        /// <summary>
        /// Calculates the pre-discount, pre-tax, pre-fee subtotal of an order.
        /// </summary>
        public Money CalculateSubtotal(IReadOnlyList<OrderItem> orderItems)
        {
            if (!orderItems.Any())
            {
                // Or handle as an error, depending on business rules.
                // Assuming a default currency can be determined.
                return Money.Zero("USD"); 
            }
            var currency = orderItems.First().LineItemTotal.Currency;
            return orderItems.Sum(item => item.LineItemTotal, currency);
        }

        /// <summary>
        /// Validates a coupon's rules and calculates the resulting discount amount.
        /// This method is pure and receives all necessary data.
        /// </summary>
        public Result<Money> ValidateAndCalculateDiscount(
            Coupon coupon,
            int currentUserUsageCount,
            IReadOnlyList<OrderItem> orderItems,
            Money subtotal)
        {
            // 1. Basic Validity Checks
            if (!coupon.IsEnabled) return Result.Failure<Money>(CouponErrors.NotActive);
            if (DateTime.UtcNow < coupon.ValidityStartDate || DateTime.UtcNow > coupon.ValidityEndDate)
                return Result.Failure<Money>(CouponErrors.Expired);

            // 2. Usage Limit Checks
            if (coupon.TotalUsageLimit.HasValue && coupon.CurrentTotalUsageCount >= coupon.TotalUsageLimit.Value)
                return Result.Failure<Money>(CouponErrors.UsageLimitExceeded);
            if (coupon.UsageLimitPerUser.HasValue && currentUserUsageCount >= coupon.UsageLimitPerUser.Value)
                return Result.Failure<Money>(CouponErrors.UserUsageLimitExceeded);

            // 3. Order Condition Checks
            if (coupon.MinOrderAmount is not null && subtotal.IsLessThan(coupon.MinOrderAmount))
                return Result.Failure<Money>(CouponErrors.MinAmountNotMet);

            // 4. Calculate Discount Base
            decimal discountBaseAmount = coupon.AppliesTo.Scope switch
            {
                CouponScope.WholeOrder => subtotal.Amount,
                CouponScope.SpecificItems => orderItems
                    .Where(oi => coupon.AppliesTo.ItemIds.Contains(oi.Snapshot_MenuItemId))
                    .Sum(oi => oi.LineItemTotal.Amount),
                CouponScope.SpecificCategories => orderItems
                    .Where(oi => coupon.AppliesTo.CategoryIds.Contains(oi.Snapshot_MenuCategoryId))
                    .Sum(oi => oi.LineItemTotal.Amount),
                _ => 0m
            };

            if (discountBaseAmount <= 0) return Result.Failure<Money>(CouponErrors.NotApplicable);

            // 5. Calculate Final Discount Value
            Money calculatedDiscount;
            switch (coupon.Type)
            {
                case CouponType.Percentage:
                    calculatedDiscount = new Money(discountBaseAmount * (coupon.Value.PercentageValue!.Value / 100m), subtotal.Currency);
                    break;
                case CouponType.FixedAmount:
                    var fixedAmount = coupon.Value.FixedAmountValue!.Amount;
                    calculatedDiscount = new Money(Math.Min(discountBaseAmount, fixedAmount), subtotal.Currency);
                    break;
                case CouponType.FreeItem:
                    var freeItem = orderItems
                        .Where(oi => oi.Snapshot_MenuItemId == coupon.Value.FreeItemValue)
                        .OrderBy(oi => oi.LineItemTotal.Amount / oi.Quantity) // Price per unit
                        .FirstOrDefault();
                    if (freeItem is null) return Result.Failure<Money>(CouponErrors.NotApplicable);
                    calculatedDiscount = new Money(freeItem.LineItemTotal.Amount / freeItem.Quantity, subtotal.Currency);
                    break;
                default:
                    return Result.Failure<Money>(CouponErrors.InvalidType);
            }
            
            // Ensure discount doesn't exceed the subtotal it applies to
            return new Money(Math.Min(calculatedDiscount.Amount, discountBaseAmount), subtotal.Currency);
        }

        /// <summary>
        /// Calculates the final total amount to be charged.
        /// </summary>
        public Money CalculateFinalTotal(Money subtotal, Money discount, Money deliveryFee, Money tip, Money tax)
        {
            var finalAmount = subtotal - discount + deliveryFee + tip + tax;
            // Ensure total is not negative
            if (finalAmount.IsLessThan(Money.Zero(finalAmount.Currency)))
            {
                return Money.Zero(finalAmount.Currency);
            }
            return finalAmount;
        }
    }
    ```

---

#### **Phase 2: Application Layer Refactoring**

**2.1. Refactor `CreateOrderCommandHandler.cs`**

* **Core Principle:** The handler is the master orchestrator of the entire business process. It follows a strict, safe sequence to ensure an "all-or-nothing" outcome.
* **Dependencies:** Repositories (`IOrderRepository`, `ICouponRepository`, etc.), `IUnitOfWork`, `IPaymentGatewayService`, and the concrete `OrderFinancialService`.
* **Revised `Handle` Method Orchestration:**

    1. **Start Transaction & Fetch Data (I/O Block 1):**
        * Begin `IUnitOfWork` transaction.
        * Fetch all required entities: `customer`, `restaurant`, `menuItems`.
        * **Perform initial business validations:** Is the customer active? Is the restaurant accepting orders? Are all menu items available? If any check fails, return an error immediately (e.g., `RestaurantErrors.NotAcceptingOrders`).
        * Create the list of `OrderItem` entities with snapshot data.

    2. **Calculate Subtotal & Prepare for Discount (In-Memory):**
        * `var subtotal = _orderFinancialService.CalculateSubtotal(orderItems);`

    3. **Handle Coupon Logic (I/O Block 2 + In-Memory):**
        * Initialize `discountAmount` to zero.
        * If a `command.CouponCode` is provided:
            * Fetch the `coupon` aggregate from the repository.
            * Fetch the `currentUserUsageCount` from a dedicated read model repository (e.g., `ICouponUsageReadRepository`).
            * Invoke the domain service: `var discountResult = _orderFinancialService.ValidateAndCalculateDiscount(...)`.
            * If `discountResult.IsFailure`, the command fails. Return the specific `CouponError`.
            * Set `discountAmount` and `appliedCouponId` from the successful result.

    4. **Calculate Final Total (In-Memory):**
        * `var totalAmount = _orderFinancialService.CalculateFinalTotal(subtotal, discountAmount, ...);`

    5. **Process Payment (I/O Block 3 - The Point of No Return):**
        * Initialize an empty `List<PaymentTransaction>`.
        * **If Online Payment:**
            * Call the payment gateway: `var paymentResult = await _paymentGatewayService.ProcessPaymentAsync(totalAmount, ...);`
            * **If `paymentResult.IsSuccess == false`:** The charge failed at the gateway. The customer was **not** charged. The command fails, and the transaction is rolled back. `return PaymentErrors.GatewayDeclined;`
            * **If `paymentResult.IsSuccess == true`:** The customer **has been charged**. Create a successful `PaymentTransaction` object with the `GatewayTransactionId` and add it to the list.
        * **If COD Payment:** Create a `CommittedToCOD` `PaymentTransaction` and add it to the list.

    6. **Create and Persist the Order (Final Step within Transaction):**
        * Invoke the `Order.Create(...)` factory method, passing all pre-calculated and validated data.
        * If `orderResult.IsFailure`, this indicates a critical internal bug. The `catch` block must handle this by initiating a refund (see below).
        * `await _orderRepository.AddAsync(orderResult.Value);`
        * The `OrderCreated` event handler is now responsible for any side effects, like calling `coupon.IncrementUsageCount()` and saving the `Coupon` aggregate.

    7. **Commit Transaction:**
        * Complete the `IUnitOfWork`. This saves the `Order` to the database and dispatches the `OrderCreated` domain event for handlers to process.

#### **Phase 3: Infrastructure and Error Handling**

* **Payment Gateway Abstraction:** Implement `IPaymentGatewayService` (e.g., `StripeService.cs`) in the Infrastructure layer. Use idempotency keys to prevent double-charges.
* **Robust Error Handling:**
  * Wrap the entire `Handle` method logic in a `try...catch` block.
  * The `catch` block is specifically for handling the rare but critical failure *after* a successful payment but *before* the database commit.
  * **Catch Block Logic:**
        1. Check if a successful `paymentResult` was received.
        2. If yes, log a **CRITICAL** error with all context (`CustomerId`, `GatewayTransactionId`, etc.).
        3. Immediately trigger a compensating action: `await _paymentGatewayService.RefundPaymentAsync(paymentResult.GatewayTransactionId, ...);`.
        4. If the refund itself fails, the critical error log is the source of truth for manual intervention by an administrator.

You are absolutely right to bring this up. The Stripe Payment Intents flow is the modern standard for a reason—it's more robust, secure, and flexible. Your analysis is spot on. This requires a significant but highly beneficial shift in our design from a synchronous "charge-then-create" model to an asynchronous, two-phase "create-then-confirm" model.

Let's thoroughly reconsider the refactoring with this superior flow as our guide.

---

### Final Outline for Refactoring with Stripe Payment Intents

This refactoring embraces the asynchronous, event-driven nature of the Payment Intents API. The `Order` aggregate becomes a state machine that reacts to payment outcomes, orchestrated by two distinct application processes.

#### **Key Design Decisions & Principles**

1. **The Webhook is the Source of Truth:** Our system will no longer assume a payment is successful based on a direct API response. The definitive outcome (`succeeded` or `failed`) will **only** come from a verified Stripe webhook.
2. **The Order is Created First:** An `Order` record is created in a `PendingPayment` state *before* the customer interacts with the Stripe payment form. This secures the order details and provides a reference (`OrderId`) for the entire process.
3. **The `Order` Aggregate is a State Machine:** The aggregate's role shifts. It will now manage transitions from `PendingPayment` to a final state (`Placed` or `PaymentFailed`) based on external events.
4. **Stateless Domain Service is Still Key:** The `OrderFinancialService` remains critical for calculating the final, trusted amount *before* the `PaymentIntent` is created.

---

#### **Phase 1: Domain Layer Refactoring**

**1.1. Refactor the `Order` Aggregate (`src/Domain/OrderAggregate/Order.cs`)**

* **Objective:** Adapt the aggregate to support the new two-phase creation and payment confirmation lifecycle.
* **Update `OrderStatus` Enum:**
  * Add new statuses: `PendingPayment`, `PaymentFailed`.
  * The lifecycle for online payments is now: `PendingPayment` -> `Placed` OR `PaymentFailed`.
  * The lifecycle for COD remains: `Placed` -> `Accepted`...
* **Properties to Add:**
  * `public string? PaymentIntentId { get; private set; }`: Stores the Stripe Payment Intent ID to link webhooks back to our order.
* **Public Methods to Reintroduce/Modify:**
  * **`public Result ConfirmPayment()`:**
    * **Purpose:** Transitions the order to a fully paid state. Called by the webhook handler.
    * **Logic:**
      * Checks if `Status == PendingPayment`. If not, `return OrderErrors.InvalidStatusForPaymentConfirmation`.
      * Sets `Status = Placed`.
      * Sets `LastUpdateTimestamp = DateTime.UtcNow`.
      * Raises a new domain event: `OrderPaymentSucceeded(Id)`.
      * Returns `Result.Success()`.
  * **`public Result MarkAsPaymentFailed()`:**
    * **Purpose:** Marks the order as failed due to a payment issue. Called by the webhook handler.
    * **Logic:**
      * Checks if `Status == PendingPayment`. If not, `return OrderErrors.InvalidStatusForPaymentConfirmation`.
      * Sets `Status = PaymentFailed`.
      * Sets `LastUpdateTimestamp = DateTime.UtcNow`.
      * Raises a new domain event: `OrderPaymentFailed(Id)`.
      * Returns `Result.Success()`.
* **`Create` Factory Method Refactoring:**
  * The `Create` method now sets the initial status based on the payment method and must store the `PaymentIntentId`.
  * **Signature:**

        ```csharp
        public static Result<Order> Create(
            ..., // customerId, restaurantId, orderItems, etc.
            Money subtotal, Money discountAmount, ..., Money totalAmount, // All pre-calculated
            CouponId? appliedCouponId,
            // --- New Parameters for Payment Intent Flow ---
            OrderStatus initialStatus,
            string? paymentIntentId = null)
        ```

  * **Internal Logic:**
    * It still performs the financial consistency checks (internal total vs. provided total).
    * It sets `Status = initialStatus`.
    * It sets `PaymentIntentId = paymentIntentId`.
    * **It does NOT create `PaymentTransaction`s yet.** These will be created in response to webhook events to accurately reflect the final outcome.

**1.2. Retain the `OrderFinancialService` (`src/Domain/Services/`)**

* This service is **unchanged** and remains essential. Its methods (`CalculateSubtotal`, `ValidateAndCalculateDiscount`, `CalculateFinalTotal`) are used by the application handler *before* creating the `PaymentIntent`.
* **Methods:**
    * `public Money CalculateSubtotal(IReadOnlyList<OrderItem> orderItems)`: Sums the `LineItemTotal` of all items.
    * `public Result<Money> ValidateAndCalculateDiscount(Coupon coupon, int currentUserUsageCount, IReadOnlyList<OrderItem> orderItems, Money subtotal)`:
      * Performs all business rule checks for a coupon: active status, validity dates, usage limits (both total and per-user), and minimum order amount.
      * Calculates the discount value based on the coupon's type (`Percentage`, `FixedAmount`, `FreeItem`) and scope (`WholeOrder`, `SpecificItems`, etc.).
      * Returns a `Result` object containing the calculated discount `Money` or a specific `CouponError`.
    * `public Money CalculateFinalTotal(Money subtotal, Money discount, Money deliveryFee, Money tip, Money tax)`: Calculates the final charge amount, ensuring it is not negative.

---

#### **Phase 2: Application Layer & Infrastructure Refactoring**

The application flow is now split into two distinct, decoupled processes.

**2.1. Process 1: Creating the Order and Payment Intent (User-Facing API)**

* **Command:** `CreateOrderAndInitiatePaymentCommand`
* **Handler Orchestration:**
    1. **Start Transaction & Fetch/Validate Data:**
        * Begin `IUnitOfWork` transaction.
        * Fetch `restaurant`, `menuItems`, etc. Validate item availability and restaurant status.
    2. **Calculate Financials (In-Memory):**
        * Use `OrderFinancialService` to calculate `subtotal`, `discountAmount`, and `totalAmount`.
    3. **Handle Payment Method Logic:**
        * **If Online Payment (Stripe):**
            * **Step 3a: Create Payment Intent (External I/O):**
                * Call `var intentResult = await _paymentGatewayService.CreatePaymentIntentAsync(totalAmount, currency, metadata: new { orderId = tempOrderId });`
                * If this fails, the command fails. No data is saved.
            * **Step 3b: Create the Order Aggregate:**
                * Call `Order.Create(..., totalAmount, initialStatus: OrderStatus.PendingPayment, paymentIntentId: intentResult.PaymentIntentId)`.
        * **If COD Payment:**
            * Call `Order.Create(..., totalAmount, initialStatus: OrderStatus.Placed)`. No `PaymentIntentId` is needed.
    4. **Persist and Commit:**
        * `await _orderRepository.AddAsync(orderResult.Value);`
        * Commit the `IUnitOfWork` transaction.
    5. **Return Response to Frontend:**
        * **For Online Payment:** The DTO must include the `client_secret` from `intentResult`.
        * **For COD Payment:** The DTO simply confirms order creation.

**2.2. Process 2: Handling the Payment Outcome (Stripe Webhook)**

* **Endpoint:** A dedicated controller, e.g., `StripeWebhookController`. This is NOT a standard MediatR command handler.
* **Webhook Handler Logic:**
    1. **Verify Signature:** The absolute first step is to validate the `Stripe-Signature` header. If invalid, return `400 Bad Request`.
    2. **Idempotency Check:** Deserialize the event. Check if the `event.Id` has been processed before (e.g., by querying a `ProcessedEvents` table). If yes, return `200 OK`.
    3. **Find the Order:**
        * Extract the `PaymentIntent` object from the event data.
        * `var order = await _orderRepository.GetByPaymentIntentIdAsync(paymentIntent.Id);`
        * If no order is found, log a warning and return `200 OK` (we can't process it, but Stripe should stop sending it).
    4. **Switch on Event Type:**
        * `case "payment_intent.succeeded":`
            * Call `var result = order.ConfirmPayment();`.
            * If `result.IsSuccess`, create a successful `PaymentTransaction` entity and add it to the order.
            * `await _orderRepository.UpdateAsync(order);`
            * The `UnitOfWork` will commit, publishing the `OrderPaymentSucceeded` event for downstream processing (notifications, revenue recording).
        * `case "payment_intent.payment_failed":`
            * Call `var result = order.MarkAsPaymentFailed();`.
            * Create a failed `PaymentTransaction` entity with the failure reason from `paymentIntent.last_payment_error`.
            * `await _orderRepository.UpdateAsync(order);`
    5. **Record Event ID:** Log the `event.Id` as processed to ensure idempotency.
    6. **Acknowledge:** Return `200 OK` to Stripe.

---

### Comparison of Flows

| Aspect | Old "Charge-First" Model | New "Payment Intent" Model |
| :--- | :--- | :--- |
| **Order Creation** | Order created **after** a successful synchronous charge. | Order created **before** payment in a `PendingPayment` state. |
| **Source of Truth** | The direct response from the `charge` API call. | The asynchronous `payment_intent.succeeded` webhook event. |
| **Resilience** | Brittle. A failure between charge and DB save requires complex refund logic. | Highly resilient. The system simply waits for the definitive outcome from Stripe. |
| **User Experience** | Can be slow if the charge API is slow. Doesn't support 3D Secure well. | Supports modern authentication (3D Secure) and other payment methods seamlessly. |
| **`Order` Aggregate**| Complex `Create` method, simpler state machine. | Simpler `Create` method, more complex state machine (`PendingPayment` state). |
| **Application Logic** | Contained within a single, complex command handler. | Split between a command handler (initiation) and a webhook handler (confirmation). |
