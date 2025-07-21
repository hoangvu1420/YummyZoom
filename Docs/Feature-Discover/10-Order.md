## Feature Discovery & Application Layer Design

### Aggregate Under Design: `Order`

### 1. Core Use Cases & Actors

***Instructions:*** *Identify who (which user role or system process) interacts with this aggregate and what their primary goals are. This helps define the scope and purpose of the features.*

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Customer` | Place a new order | The primary action of creating an order with items, address, payment, and an optional coupon. This can be from a personal cart or a `TeamCart`. |
| `Customer` | Track an existing order | View the real-time status of a placed order (`Accepted`, `Preparing`, etc.) and the estimated delivery time. |
| `Customer` | View order history | Browse a list of all past orders and view their detailed information. |
| `Customer` | Reorder a past order | Initiate a new order using the items from a previously completed order, allowing for quick re-purchasing. |
| `Customer` | Cancel an order | Cancel an order shortly after placing it, before it has progressed too far in the fulfillment process. |
| `Restaurant Staff` | Receive and manage new orders | View incoming orders in real-time on a dashboard. |
| `Restaurant Staff` | Accept or reject an order | Confirm that the restaurant can fulfill an order and provide an ETA, or reject it if unable to fulfill. |
| `Restaurant Staff` | Update order status | Move an accepted order through the fulfillment lifecycle: `Preparing` → `Ready for Delivery` → `Delivered`. |
| `Admin` | Monitor and manage all orders | View any order in the system to provide support, resolve issues, or oversee platform activity. |
| `Admin` | Manually update or cancel an order | Intervene in the order lifecycle on behalf of a customer or restaurant, for example, to process a cancellation or refund. |
| `System (Event Handler)` | Process post-order side effects | Decoupled processes that run after an order's state changes, such as sending notifications, processing payments, recording financial transactions, and enabling reviews. |

---

### 2. Commands (Write Operations)

***Instructions:*** *List all actions that will create or change the state of this aggregate. Each command represents a single, atomic use case from the Application Layer's perspective.*

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| **`InitiateOrderCommand`** | `Customer` | `CustomerId`, `RestaurantId`, `List<OrderItemDto>`, `CouponCode?`, `TipAmount?`, `PaymentMethodType` | `InitiateOrderResponse(OrderId, ClientSecret?)` | `Customer` role. Must be the authenticated user. |
| **`HandleStripeWebhookCommand`** | `Stripe (System)` | `JsonPayload`, `SignatureHeader` | `Result.Success()` | Public endpoint, validated by Stripe signature. |
| **`AcceptOrderCommand`** | `Restaurant Staff` | `OrderId`, `EstimatedDeliveryTime` | `Result.Success()` | `Restaurant Staff/Owner` role. Must be associated with the order's restaurant. |
| **`RejectOrderCommand`** | `Restaurant Staff` | `OrderId`, `RejectionReason` (optional) | `Result.Success()` | `Restaurant Staff/Owner` role. Must be associated with the order's restaurant. |
| **`UpdateOrderStatusToPreparingCommand`** | `Restaurant Staff` | `OrderId` | `Result.Success()` | `Restaurant Staff/Owner` role. Must be associated with the order's restaurant. |
| **`UpdateOrderStatusToReadyForDeliveryCommand`**| `Restaurant Staff` | `OrderId` | `Result.Success()` | `Restaurant Staff/Owner` role. Must be associated with the order's restaurant. |
| **`UpdateOrderStatusToDeliveredCommand`**| `Restaurant Staff` | `OrderId` | `Result.Success()` | `Restaurant Staff/Owner` role. Must be associated with the order's restaurant. |
| **`CancelOrderCommand`** | `Customer`, `Admin`, `Restaurant Staff` | `OrderId`, `CancelledByUserId`, `Reason` (optional) | `Result.Success()` | `Customer` (must own order), `Restaurant Staff` (must be for their restaurant), or `Admin`. |
| **`InitiateReorderCommand`** | `Customer` | `OrderId` | `ReorderCartDto` (pre-filled cart data) | `Customer` role. Must own the original order. |
| **`ConfirmCodPaymentCommand`** | `Restaurant Staff`, `Admin` | `OrderId` | `Result.Success()` | `Restaurant Staff/Owner` or `Admin` role. |

---

### 3. Queries (Read Operations)

***Instructions:*** *List all the ways data needs to be retrieved for this aggregate. Remember, queries use Dapper/SQL for performance and can join across tables to create tailored DTOs.*

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL Highlights / Key Tables |
| :--- | :--- | :--- | :--- | :--- |
| **`GetOrderDetailsQuery`** | `Customer`, `Restaurant Staff`, `Admin` | `OrderId` | `OrderDetailsDto` | `SELECT ... FROM "Orders" o JOIN "OrderItems" oi ON o.Id = oi.OrderId LEFT JOIN "Users" u ON o.CustomerId = u.Id WHERE o.Id = @OrderId`. Includes item snapshots, customizations, address, and status. |
| **`GetMyOrdersQuery`** | `Customer` | `CustomerId`, `PaginationOptions` | `PaginatedList<OrderSummaryDto>` | `SELECT Id, OrderNumber, Status, TotalAmount, PlacementTimestamp, RestaurantName FROM "OrdersView" WHERE CustomerId = @CustomerId ORDER BY PlacementTimestamp DESC`. Uses a denormalized read model view. |
| **`GetRestaurantOrdersQuery`**| `Restaurant Staff` | `RestaurantId`, `PaginationOptions`, `OrderStatusFilter[]` | `PaginatedList<RestaurantOrderDto>` | `SELECT ... FROM "Orders" WHERE RestaurantId = @RestaurantId AND Status IN @OrderStatusFilter ORDER BY PlacementTimestamp ASC`. Optimized for the restaurant dashboard. |
| **`GetOrderStatusQuery`** | `Customer` (for tracking UI) | `OrderId` | `OrderStatusDto` | `SELECT Status, EstimatedDeliveryTime, LastUpdateTimestamp FROM "Orders" WHERE Id = @OrderId`. A lightweight query for polling. |
| **`SearchAllOrdersQuery`** | `Admin` | `SearchTerm`, `Filters` (Date, Status, Restaurant), `PaginationOptions` | `PaginatedList<AdminOrderSummaryDto>` | `SELECT ... FROM "Orders" o JOIN "Users" u ... JOIN "Restaurants" r ... WHERE ...`. Complex query for the admin panel. |

---

### 4. Domain Event Handling

***Instructions:*** *Identify the domain events this aggregate raises. For each event, list the decoupled side effects (handlers) that should run. This is key for building a loosely coupled system.*

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| **`OrderInitiated`** | `InitiateOrderCommand` | `(Optional) NotifyCustomerOrderIsPending` | Sends a notification that their order is pending payment confirmation. |
| **`OrderPaymentSucceeded`** | `HandleStripeWebhookCommand` | `NotifyRestaurantOfNewOrder` | **(Critical)** Sends a WebSocket/push notification to the restaurant's dashboard. This is the trigger for fulfillment. |
| `OrderPaymentSucceeded` | `HandleStripeWebhookCommand` | `SendOrderConfirmationToCustomer` | Sends a confirmation email/SMS to the customer with order details. |
| `OrderPaymentSucceeded` | `HandleStripeWebhookCommand` | `UpdateCouponUsageOnPaymentSuccess` | Finds the `Coupon` aggregate, calls `incrementUsageCount()`, and saves it. Also logs usage in the read model. |
| `OrderPaymentSucceeded` | `HandleStripeWebhookCommand` | `RecordRevenueForRestaurant` | Finds the `RestaurantAccount` aggregate, calls `RecordRevenue()`, and saves it. |
| **`OrderPaymentFailed`** | `HandleStripeWebhookCommand` | `NotifyCustomerPaymentFailed` | Informs the customer that their payment failed and encourages them to retry. |
| **`CodOrderPlaced`** | `InitiateOrderCommand` (for COD) | `NotifyRestaurantOfNewOrder` | For COD, this event directly triggers the restaurant notification. |
| **`CodPaymentConfirmed`** | `ConfirmCodPaymentCommand` | `RecordRevenueForRestaurant` | Completes the financial loop for COD orders. |
| **`OrderAccepted`** | `AcceptOrderCommand` | `NotifyCustomerOnOrderAccepted` | Sends a notification to the customer with the `EstimatedDeliveryTime`. |
| **`OrderRejected`** | `RejectOrderCommand` | `InitiateRefundForRejectedOrder` | Triggers a refund process via the payment gateway if the order was pre-paid. |
| **`OrderCancelled`** | `CancelOrderCommand` | `NotifyStakeholdersOnOrderCancellation` | Informs both customer and restaurant that the order has been cancelled. |
| `OrderCancelled` | `CancelOrderCommand` | `InitiateRefundForCancelledOrder` | Similar to the handler for `OrderRejected`, triggers a refund if applicable. |
| **`OrderDelivered`** | `UpdateOrderStatusToDeliveredCommand` | `EnableReviewForCustomer` | Creates or updates a record in a `ReviewEligibility` read model, allowing the UI to show a "Leave a Review" button for this order. |
| `OrderDelivered` | `UpdateOrderStatusToDeliveredCommand` | `SendDeliveryConfirmationToCustomer` | Sends a final confirmation email/notification to the customer. |
| **`OrderPreparing`**, **`OrderReadyForDelivery`** | `UpdateOrderStatus...` | `NotifyCustomerOnStatusUpdate` | Sends a notification to the customer, keeping them informed of the order's progress. |

---

### 5. Key Business Logic & Application Service Orchestration

***Instructions:*** *For the most complex command(s), outline the step-by-step logic inside the command handler. This clarifies the orchestration of repository calls, domain service interactions, and aggregate method invocations.*

#### **Process 1: `InitiateOrderCommandHandler` Orchestration (User-Facing API)**

This handler creates the initial order record and, if necessary, a payment intent.

1.  **Start Transaction & Fetch/Validate Data:**
    *   Begin a transaction using `IUnitOfWork`.
    *   Fetch `restaurant`, `menuItems`, etc. Validate item availability and restaurant status.
2.  **Calculate Financials (In-Memory):**
    *   Use the `OrderFinancialService` to calculate `subtotal`, `discountAmount`, and `totalAmount`.
3.  **Handle Payment Method Logic:**
    *   **If Online Payment (Stripe):**
        *   **Create Payment Intent (External I/O):** Call `_paymentGatewayService.CreatePaymentIntentAsync(totalAmount, ...)`. If this fails, the command fails.
        *   **Create the Order Aggregate:** Call `Order.Create(..., initialStatus: OrderStatus.PendingPayment, paymentIntentId: intentResult.PaymentIntentId)`.
    *   **If COD Payment:**
        *   Call `Order.Create(..., initialStatus: OrderStatus.Placed)`. No `PaymentIntent` is created.
4.  **Persist and Commit:**
    *   `await _orderRepository.AddAsync(orderResult.Value);`
    *   Commit the `IUnitOfWork` transaction. This will dispatch `OrderInitiated` (for online) or `CodOrderPlaced` (for COD).
5.  **Return Response to Frontend:**
    *   **For Online Payment:** Return an `InitiateOrderResponse` containing the `OrderId` and the `client_secret` from the payment intent.
    *   **For COD Payment:** Return an `InitiateOrderResponse` with just the `OrderId`.

#### **Process 2: `HandleStripeWebhookCommandHandler` Orchestration (System-to-System)**

This handler acts as the trusted listener for the definitive payment outcome from Stripe.

1.  **Verify Signature & Deserialize:** The handler receives the raw JSON payload and signature. It uses `IPaymentGatewayService` to verify the signature and deserialize the event. If invalid, it fails.
2.  **Idempotency Check:** It checks a persistent store (e.g., `ProcessedEvents` table) to see if this `event.Id` has already been handled. If so, it returns success immediately.
3.  **Find the Order:**
    *   Extract the `PaymentIntent.Id` from the event data.
    *   Fetch the order: `var order = await _orderRepository.GetByPaymentIntentIdAsync(paymentIntent.Id);`. If not found, log a warning and exit successfully.
4.  **Invoke Aggregate Method based on Event Type:**
    *   `case "payment_intent.succeeded":`
        *   Call `var result = order.ConfirmPayment();`.
        *   If `result.IsSuccess`, create a successful `PaymentTransaction` entity and add it to the order.
    *   `case "payment_intent.payment_failed":`
        *   Call `var result = order.MarkAsPaymentFailed();`.
        *   Create a failed `PaymentTransaction` with the failure reason.
5.  **Persist Changes and Log Event:**
    *   `await _orderRepository.UpdateAsync(order);`
    *   Log the `event.Id` to the `ProcessedEvents` table.
    *   Commit the `UnitOfWork`. This dispatches `OrderPaymentSucceeded` or `OrderPaymentFailed`.

---

### 6. Design Notes & Suggestions

***Instructions:*** *Add any additional notes, considerations, or suggestions for the design of this aggregate. This could include performance considerations, future extensibility, or architectural patterns.*

1.  **Clear Separation of Concerns:** This refactored design is a prime example of DDD principles.
    *   **Application Layer (`CreateOrderCommandHandler`):** Acts as a pure orchestrator of a business process.
    *   **Domain Service (`OrderFinancialService`):** Encapsulates complex, stateless business logic and calculations.
    *   **Aggregate (`Order`):** Protects the integrity of a transactional record and manages its post-creation lifecycle.
2.  **Enhanced Transactional Integrity:** The "all-or-nothing" nature of order creation is now explicit. An `Order` is only ever created if all preconditions—item availability, coupon validity, and successful payment—have been met. This dramatically reduces the risk of data inconsistencies.
3.  **Aggregate Immutability:** The `Order` aggregate is now truly immutable upon creation regarding its financial details and items. This makes it a reliable and auditable historical record, which is critical for a food ordering platform.
4.  **Handling the "Point of No Return":** The design correctly identifies that the moment a payment succeeds is the most critical point. Robust `try...catch` logic in the command handler is essential to trigger a compensating action (an immediate refund) if any subsequent step fails, preventing "lost money" scenarios.
5.  **Extensibility with the Saga Pattern:** The post-creation lifecycle (`Accepted` -> `Delivered`) is still a good candidate for the Saga pattern if coordination with external services (like a delivery fleet) is required in the future.
6.  **Concurrency Management:** The risk of an item's availability changing during checkout remains. An optimistic concurrency check on `MenuItem`s or a Reservation Pattern are potential future enhancements for high-volume scenarios.

### 7. Order Payment Process & Gateway Abstraction

This section details the orchestration of payments within the `CreateOrderCommandHandler`, outlining the two primary methods: Cash on Delivery (COD) and Online Payments (via Stripe). The core principle is that the Application Layer handles the payment interaction and prepares the necessary data, while the `Order` aggregate simply validates the financial consistency of the result.

#### **Payment Gateway Abstraction (`IPaymentGatewayService`)**

To keep the application decoupled from a specific payment provider like Stripe, we define an abstraction in the Application Layer, with its implementation in the Infrastructure Layer.

**Interface Definition (`src/Application/Common/Interfaces/IPaymentGatewayService.cs`)**
```csharp
// A record to standardize the result from any payment provider.
public record PaymentIntentResult(string PaymentIntentId, string ClientSecret);

public record RefundResult(
    bool IsSuccess,
    string GatewayRefundId,
    string? FailureReason = null);

public interface IPaymentGatewayService
{
    /// <summary>
    /// Creates a PaymentIntent with the payment provider.
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(Money amount, string currency, IDictionary<string, string> metadata);

    /// <summary>
    /// Verifies the webhook signature and deserializes the event.
    /// </summary>
    Result<StripeEvent> ConstructWebhookEvent(string json, string stripeSignatureHeader);

    /// <summary>
    /// Processes a refund for a previously successful transaction.
    /// </summary>
    /// <param name="gatewayTransactionId">The original transaction ID to refund.</param>
    /// <param name="amountToRefund">The amount to refund.</param>
    /// <param name="reason">The reason for the refund.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A standardized RefundResult.</returns>
    Task<RefundResult> RefundPaymentAsync(
        string gatewayTransactionId,
        Money amountToRefund,
        string reason,
        CancellationToken cancellationToken = default);
}
```

---

#### **Scenario 1: Cash on Delivery (COD) Payment Flow**

The COD flow is now a simplified path within the `InitiateOrderCommand`. The order is created directly with a `Placed` status, and no `PaymentIntent` is involved. The `CodOrderPlaced` event triggers the fulfillment process immediately.

#### **Scenario 2: Online Payment (Stripe Payment Intent Flow)**

This is now a two-step, asynchronous process:

1.  **Initiation (User-driven):**
    *   The `InitiateOrderCommandHandler` validates the cart, calculates the final `totalAmount`, and calls `_paymentGatewayService.CreatePaymentIntentAsync(...)`.
    *   It creates the `Order` in the database with `Status = PendingPayment` and stores the `PaymentIntentId`.
    *   It returns the `client_secret` to the frontend.
2.  **Confirmation (Frontend & Stripe-driven):**
    *   The frontend uses the `client_secret` to confirm the payment with Stripe. The user may go through 3D Secure authentication.
    *   Stripe processes the payment and sends a `payment_intent.succeeded` (or `failed`) webhook to our backend.
3.  **Finalization (System-driven):**
    *   The `HandleStripeWebhookCommandHandler` receives the webhook.
    *   It finds the corresponding `Order` via the `PaymentIntentId`.
    *   It calls `order.ConfirmPayment()`, which changes the status to `Placed`.
    *   It saves the order, which dispatches the `OrderPaymentSucceeded` event.
    *   **Only now** do the downstream processes (notifying the restaurant, recording revenue) begin.
