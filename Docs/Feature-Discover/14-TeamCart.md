## Feature Discovery & Application Layer Design

### Aggregate Under Design: `TeamCart`

### 1. Core Use Cases & Actors

| Actor (Role) | Use Case / Goal | Description |
| :--- | :--- | :--- |
| `Customer (Host)` | **Create a Team Cart** | Starts a new collaborative cart for a specific restaurant, generating a shareable link. |
| `Customer (Host)` | **Lock Cart for Payment** | Ends the item-adding phase, freezes the cart's contents, and moves it to the payment phase for all members. |
| `Customer (Host)` | **Apply Tip or Coupon** | **After locking the cart**, adds a tip or applies a valid coupon code to the entire group order. |
| `Customer (Host)` | **Confirm and Place Final Order** | After all payments are settled, gives the final confirmation that converts the `TeamCart` into a permanent `Order`. |
| `Customer (Guest)` | **Join a Team Cart** | Uses a shared link to access the `TeamCart` and become a member. |
| `Customer (Guest)` | **Add/Update/Remove Items** | During the `Open` phase, adds their desired food items and customizations to the shared cart. |
| `Customer (Guest)` | **Commit to Payment** | **After the cart is locked**, chooses a payment method (COD) or completes an online payment for their final, calculated portion. |
| `System (Scheduler)` | **Expire Abandoned Team Carts** | A background job that finds and marks old `TeamCart`s as `Expired`. |
| `System (Event Handler)` | **Update Real-time View Model** | Listens to `TeamCart` domain events to update a denormalized read model (e.g., in Redis) for a live experience. |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| **`CreateTeamCartCommand`** | `Customer (Host)`| `RestaurantId`, `HostName` | `CreateTeamCartResponse(TeamCartId, ShareToken)` | `Customer` role. |
| **`AddMemberToTeamCartCommand`** | `Customer (Guest)`| `TeamCartId`, `ShareToken`, `GuestUserId`, `GuestName` | `Result.Success()` | `Customer` role. |
| **`AddItemToTeamCartCommand`** | `Customer` | `TeamCartId`, `UserId`, `MenuItemId`, `Quantity`, `CustomizationsDto[]` | `Result.Success()` | `Customer` role, must be a member, cart must be `Open`. |
| **`UpdateTeamCartItemQuantityCommand`**|`Customer`       | `TeamCartId`, `UserId`, `TeamCartItemId`, `NewQuantity`                     | `Result.Success()`                               | `Customer` role, must own the specific `TeamCartItem`.                     |
| **`RemoveItemFromTeamCartCommand`**   | `Customer`       | `TeamCartId`, `UserId`, `TeamCartItemId`                                    | `Result.Success()`                               | `Customer` role, must own the specific `TeamCartItem`.                     |
| **`LockTeamCartForPaymentCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId` | `Result.Success()` | `Customer` role, must be the `Host` of the `TeamCart`. |
| **`CommitToCodPaymentCommand`** | `Customer` | `TeamCartId`, `UserId` | `Result.Success()` | `Customer` role, must be a member, cart must be `Locked`. |
| **`ProcessGuestOnlinePaymentCommand`** | `Customer (Guest)` | `TeamCartId`, `UserId`, `PaymentToken` | `PaymentStatusResponse` | `Customer` role, must be a member, cart must be `Locked`. |
| **`ApplyTipToTeamCartCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId`, `TipAmount` | `Result.Success()` | `Customer` role, must be the `Host`, cart must be `Locked`. |
| **`ApplyCouponToTeamCartCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId`, `CouponCode` | `Result.Success()` | `Customer` role, must be the `Host`, cart must be `Locked`. |
| **`ConvertTeamCartToOrderCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId`, `DeliveryAddressDto`, `SpecialInstructions` | `ConvertTeamCartResponse(OrderId)` | `Customer` role, must be the `Host`, cart must be `ReadyToConfirm`. |
| **`ExpireTeamCartsCommand`** | `System (Scheduler)` | (none) | `ExpireTeamCartsResponse(Count)` | System-level, internal authentication. |

---

### 3. Queries (Read Operations)

| Query Name                      | Actor / Trigger  | Key Parameters         | Response DTO                               | SQL Highlights / Key Tables                                                                                             |
| :------------------------------ | :--------------- | :--------------------- | :----------------------------------------- | :---------------------------------------------------------------------------------------------------------------------- |
| **`GetTeamCartDetailsQuery`**   | `Customer (Member)`| `TeamCartId`           | `TeamCartDetailsDto`                       | `SELECT ... FROM "TeamCarts" LEFT JOIN "TeamCartMembers", "TeamCartItems", "MemberPayments" WHERE "Id" = @TeamCartId`. This is the primary query. |
| **`GetTeamCartRealTimeViewModelQuery`**| `Customer (Member)`| `TeamCartId`           | `TeamCartViewModel` (from Redis/cache)   | This query should **NOT** hit the main SQL database. It reads from a fast, denormalized cache like Redis. |
| **`FindTeamCartByTokenQuery`**  | `Customer (Guest)`| `ShareToken`           | `TeamCartId`                               | `SELECT "Id" FROM "TeamCarts" WHERE "ShareToken" = @ShareToken AND "ExpiresAt" > NOW()`. A very fast, indexed lookup. |

#### DTO Spotlight: `TeamCartDetailsDto`
This DTO is crucial for rendering the UI.
```csharp
public record TeamCartDetailsDto(
    Guid Id,
    string RestaurantName,
    Guid RestaurantId,
    string Status, // "Open", "Locked", "ReadyToConfirm"
    bool IsHost, // Flag for the frontend to show host-specific controls
    List<MemberDto> Members,
    List<ItemDto> Items,
    FinancialsDto Financials
);

public record MemberDto(Guid UserId, string Name, string PaymentStatus, decimal AmountOwed);
public record ItemDto(Guid ItemId, Guid AddedByUserId, string Name, decimal Price, int Quantity, ...);
public record FinancialsDto(decimal Subtotal, decimal Tip, decimal Discount, decimal Total);
```

---

### 4. Domain Event Handling

| Domain Event | Triggering Command | Asynchronous Handler(s) | Handler's Responsibility |
| :--- | :--- | :--- | :--- |
| `TeamCartCreated` | `CreateTeamCartCommand` | `UpdateTeamCartViewModelHandler` | Creates the initial version of the real-time `TeamCartViewModel`. |
| `ItemAddedToTeamCart` | `AddItemToTeamCartCommand`| `UpdateTeamCartViewModelHandler` | Adds the item to the `TeamCartViewModel`. |
| **`TeamCartLockedForPayment`** | **`LockTeamCartForPaymentCommand`** | `NotifyMembersToPayHandler` | Sends a push notification to **all members**: "The Team Cart is locked! Please complete your payment." |
| `MemberCommittedToPayment` | `CommitToCodPaymentCommand` | `UpdateTeamCartViewModelHandler` | Updates the member's payment status in the real-time view model. |
| `OnlinePaymentSucceeded` | `ProcessGuestOnlinePaymentCommand` | `UpdateTeamCartViewModelHandler` | Updates the member's payment status to "Paid" in the real-time view model. |
| `TeamCartReadyForConfirmation`| (Internal transition) | `NotifyHostCartIsReadyHandler` | Sends a push notification to the Host: "Your Team Cart is ready! All members have paid." |
| **`TeamCartConverted`** | **`ConvertTeamCartToOrderCommand`** | `ArchiveTeamCartViewModelHandler` | Removes the `TeamCartViewModel` from the real-time cache. |
| `TeamCartConverted` | `ConvertTeamCartToOrderCommand` | `NotifyGroupOnOrderPlacedHandler` | Sends a notification to **all members**: "Your group order has been placed!" |

---

### 5. Key Business Logic & Application Service Orchestration

#### **`ProcessGuestOnlinePaymentCommandHandler` Orchestration:**

This handler manages a single member's online payment against a *locked* cart.

1.  **Validate** the command's input.
2.  **Start Transaction.**
3.  **Fetch `TeamCart`:** `var teamCart = await _teamCartRepository.GetByIdAsync(command.TeamCartId);`.
4.  **Perform Business Checks:**
    *   `if (teamCart.Status != TeamCartStatus.Locked) return Failure(TeamCartErrors.CanOnlyPayOnLockedCart);`
    *   Verify the user is a member of the cart.
5.  **Calculate Amount Owed (Crucial Security Step):**
    *   The handler calculates the exact amount this specific user owes based on their items and their proportional share of the host-applied tip and discount. This is the **trusted, backend-calculated amount.**
6.  **Call Payment Gateway:**
    *   `var paymentResult = await _paymentGatewayService.ProcessPaymentAsync(trustedAmount, command.PaymentToken, ...);`
    *   If `paymentResult.IsFailure`, return an error. The customer was not charged.
7.  **Invoke the Aggregate's Method:**
    *   The handler now has a successful `transactionId` from the payment gateway.
    *   `var result = teamCart.RecordSuccessfulOnlinePayment(command.UserId, trustedAmount, paymentResult.GatewayTransactionId);`
    *   If `result.IsFailure` (e.g., user already paid), the `catch` block must trigger a refund.
8.  **Persist and Complete:**
    *   `await _teamCartRepository.UpdateAsync(teamCart);`
    *   Commit the transaction.
9.  **Return** `Result.Success()`.

#### **`ConvertTeamCartToOrderCommandHandler` Orchestration:**

This handler's logic remains the same as in the previous, correct design. It is triggered by the Host *after* the cart is `ReadyToConfirm`.

1.  **Start Transaction.**
2.  **Fetch `TeamCart`**.
3.  **Authorize Host**.
4.  **Invoke `TeamCartConversionService`**:
    *   `var conversionResult = _conversionService.ConvertToOrder(teamCart, ...);`
    *   If `conversionResult.IsFailure`, return error.
5.  **Persist both aggregates**:
    *   `await _orderRepository.AddAsync(newOrder);`
    *   `await _teamCartRepository.UpdateAsync(updatedTeamCart);`
6.  **Commit Transaction.**
7.  **Return** the `OrderId`.
