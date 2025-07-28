## Feature Discovery & Application Layer Design

### Aggregate Under Design: `TeamCart`

### 1. Core Use Cases & Actors

The `TeamCart` feature is designed around a "Lock, Settle, Convert" lifecycle, ensuring a clear and orderly process from creation to final order placement.

| Actor (Role) | Use Case / Goal | Description | Lifecycle Phase |
| :--- | :--- | :--- | :--- |
| `Customer (Host)` | **Create a Team Cart** | Starts a new collaborative cart for a specific restaurant, generating a shareable link. | `Open` |
| `Customer (Guest)` | **Join a Team Cart** | Uses a shared link to access the `TeamCart` and become a member. | `Open` |
| `Customer (Member)` | **Add/Update/Remove Items** | Adds their desired food items and customizations to the shared cart. | `Open` |
| `Customer (Host)` | **Lock Cart for Payment** | Ends the item-adding phase, freezes the cart's contents, and moves it to the "Settle Up" phase for all members. | `Open` -> `Locked` |
| `Customer (Host)` | **Apply Tip or Coupon** | **After locking the cart**, adds a tip or applies a valid coupon code to the entire group order. | `Locked` |
| `Customer (Member)` | **Commit to Payment** | **After the cart is locked**, chooses a payment method (COD) or completes an online payment for their final, calculated portion. | `Locked` |
| `Customer (Host)` | **Confirm and Place Final Order** | After all payments are settled (`ReadyToConfirm`), gives the final confirmation that converts the `TeamCart` into a permanent `Order`. | `ReadyToConfirm` -> `Converted` |
| `System (Scheduler)` | **Expire Abandoned Team Carts** | A background job that finds and marks old `TeamCart`s as `Expired` if they are not converted in time. | `Any` -> `Expired` |
| `System (Event Handler)` | **Update Real-time View Model** | Listens to `TeamCart` domain events to update a denormalized read model for a live, collaborative experience. | `All Phases` |

---

### 2. Commands (Write Operations)

| Command Name | Actor / Trigger | Key Parameters | Response DTO | Authorization |
| :--- | :--- | :--- | :--- | :--- |
| **`CreateTeamCartCommand`** | `Customer (Host)`| `RestaurantId`, `HostName` | `CreateTeamCartResponse(TeamCartId, ShareToken)` | `Customer` role. |
| **`AddMemberToTeamCartCommand`** | `Customer (Guest)`| `TeamCartId`, `ShareToken`, `GuestUserId`, `GuestName` | `Result.Success()` | `Customer` role, cart must be `Open`. |
| **`AddItemToTeamCartCommand`** | `Customer (Member)` | `TeamCartId`, `UserId`, `MenuItemId`, `Quantity`, `CustomizationsDto[]` | `Result.Success()` | `Customer` role, must be a member, cart must be `Open`. |
| **`UpdateTeamCartItemQuantityCommand`**|`Customer (Member)`| `TeamCartId`, `UserId`, `TeamCartItemId`, `NewQuantity` | `Result.Success()` | `Customer` role, must own the item, cart must be `Open`. |
| **`RemoveItemFromTeamCartCommand`** | `Customer (Member)`| `TeamCartId`, `UserId`, `TeamCartItemId` | `Result.Success()` | `Customer` role, must own the item, cart must be `Open`. |
| **`LockTeamCartForPaymentCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId` | `Result.Success()` | `Customer` role, must be the `Host`, cart must be `Open`. |
| **`ApplyTipToTeamCartCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId`, `TipAmount` | `Result.Success()` | `Customer` role, must be the `Host`, cart must be `Locked`. |
| **`ApplyCouponToTeamCartCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId`, `CouponCode` | `Result.Success()` | `Customer` role, must be the `Host`, cart must be `Locked`. |
| **`RemoveCouponFromTeamCartCommand`**| `Customer (Host)`| `TeamCartId`, `HostUserId` | `Result.Success()` | `Customer` role, must be the `Host`. |
| **`CommitToCodPaymentCommand`** | `Customer (Member)` | `TeamCartId`, `UserId` | `Result.Success()` | `Customer` role, must be a member, cart must be `Locked`. |
| **`ProcessGuestOnlinePaymentCommand`** | `Customer (Member)` | `TeamCartId`, `UserId`, `PaymentToken` | `PaymentStatusResponse` | `Customer` role, must be a member, cart must be `Locked`. |
| **`ConvertTeamCartToOrderCommand`** | `Customer (Host)`| `TeamCartId`, `HostUserId`, `DeliveryAddressDto`, `SpecialInstructions` | `ConvertTeamCartResponse(OrderId)` | `Customer` role, must be the `Host`, cart must be `ReadyToConfirm`. |
| **`ExpireTeamCartsCommand`** | `System (Scheduler)` | (none) | `ExpireTeamCartsResponse(Count)` | System-level, internal authentication. |

---

### 3. Queries (Read Operations)

| Query Name | Actor / Trigger | Key Parameters | Response DTO | SQL/Cache Highlights |
| :--- | :--- | :--- | :--- | :--- |
| **`GetTeamCartDetailsQuery`** | `Customer (Member)`| `TeamCartId` | `TeamCartDetailsDto` | Primary query for full state. `SELECT ... FROM "TeamCarts" LEFT JOIN "TeamCartMembers", "TeamCartItems", "MemberPayments" WHERE "Id" = @TeamCartId`. |
| **`GetTeamCartRealTimeViewModelQuery`**| `Customer (Member)`| `TeamCartId` | `TeamCartViewModel` (from Redis/cache) | This query **MUST NOT** hit the main SQL database. It reads from a fast, denormalized cache like Redis for live UI updates. |
| **`FindTeamCartByTokenQuery`** | `Customer (Guest)`| `ShareToken` | `TeamCartId` | Fast, indexed lookup: `SELECT "Id" FROM "TeamCarts" WHERE "ShareToken" = @ShareToken AND "ExpiresAt" > NOW()`. |

#### DTO Spotlight: `TeamCartDetailsDto`
This DTO is crucial for rendering the UI and showing host-specific controls.
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
| `ItemAddedToTeamCart` | `AddItemToTeamCartCommand`| `UpdateTeamCartViewModelHandler` | Adds the item to the `TeamCartViewModel` and recalculates totals. |
| **`TeamCartLockedForPayment`** | **`LockTeamCartForPaymentCommand`** | `NotifyMembersToPayHandler` | Sends a push notification to **all members**: "The cart is locked! Please complete your payment." |
| `MemberCommittedToPayment` | `CommitToCodPaymentCommand` | `UpdateTeamCartViewModelHandler` | Updates the member's payment status to "Committed to COD" in the real-time view model. |
| `OnlinePaymentSucceeded` | `ProcessGuestOnlinePaymentCommand` | `UpdateTeamCartViewModelHandler` | Updates the member's payment status to "Paid Online" in the real-time view model. |
| `TeamCartReadyForConfirmation`| (Internal transition) | `NotifyHostCartIsReadyHandler` | Sends a push notification to the **Host**: "Your Team Cart is ready! All members have paid." |
| **`TeamCartConverted`** | **`ConvertTeamCartToOrderCommand`** | `ArchiveTeamCartViewModelHandler` | Removes the `TeamCartViewModel` from the real-time cache. |
| `TeamCartConverted` | `ConvertTeamCartToOrderCommand` | `NotifyGroupOnOrderPlacedHandler` | Sends a notification to **all members**: "Your group order has been placed!" |
| `TeamCartExpired` | `ExpireTeamCartsCommand` | `ArchiveTeamCartViewModelHandler`, `NotifyHostCartExpiredHandler` | Archives the view model and notifies the host that the cart has expired. |

---

### 5. Key Business Logic & Application Service Orchestration

Application command handlers orchestrate the "Lock, Settle, Convert" lifecycle by fetching the aggregate, calling its state transition methods, and persisting the result.

#### **Phase 1 -> 2: `LockTeamCartForPaymentCommandHandler` Orchestration**

This handler initiates the "Settle Up" phase.

1.  **Validate** the command and **authorize** that the `HostUserId` is the one making the request.
2.  **Start Transaction.**
3.  **Fetch `TeamCart`:** `var teamCart = await _teamCartRepository.GetByIdAsync(command.TeamCartId);`.
4.  **Invoke the Aggregate's Method:** `var result = teamCart.LockForPayment(command.HostUserId);`.
    *   The aggregate internally checks that the requester is the host, the cart is `Open`, and it's not empty.
    *   It changes its `Status` from `Open` to `Locked`.
    *   It raises the `TeamCartLockedForPayment` domain event.
5.  **Persist and Complete:**
    *   `await _teamCartRepository.UpdateAsync(teamCart);`
    *   Commit the transaction.
6.  **Return** `Result.Success()`. The `NotifyMembersToPayHandler` will be triggered asynchronously.

#### **Phase 2: `ProcessGuestOnlinePaymentCommandHandler` Orchestration**

This handler manages a single member's online payment against a *locked* cart.

1.  **Validate** the command's input.
2.  **Start Transaction.**
3.  **Fetch `TeamCart`:** `var teamCart = await _teamCartRepository.GetByIdAsync(command.TeamCartId);`.
4.  **Perform Business Checks:**
    *   `if (teamCart.Status != TeamCartStatus.Locked) return Failure(...)`
    *   Verify the user is a member of the cart.
5.  **Calculate Amount Owed (Crucial Security Step):**
    *   The handler calculates the exact amount this specific user owes based on their items and their proportional share of the host-applied tip and discount. This is the **trusted, backend-calculated amount.**
6.  **Call Payment Gateway:**
    *   `var paymentResult = await _paymentGatewayService.ProcessPaymentAsync(trustedAmount, command.PaymentToken, ...);`
    *   If `paymentResult.IsFailure`, return an error. The customer was not charged.
7.  **Invoke the Aggregate's Method:**
    *   `var result = teamCart.RecordSuccessfulOnlinePayment(command.UserId, trustedAmount, paymentResult.GatewayTransactionId);`
    *   The aggregate adds a `MemberPayment` and internally checks if this was the last payment needed, potentially transitioning its status to `ReadyToConfirm` and raising the `TeamCartReadyForConfirmation` event.
8.  **Persist and Complete:**
    *   `await _teamCartRepository.UpdateAsync(teamCart);`
    *   Commit the transaction.
9.  **Return** `Result.Success()`.

#### **Phase 3 -> 4: `ConvertTeamCartToOrderCommandHandler` Orchestration**

This handler executes the final conversion, triggered by the Host *after* the cart is `ReadyToConfirm`.

1.  **Start Transaction.**
2.  **Fetch `TeamCart`**.
3.  **Authorize Host**.
4.  **Invoke `TeamCartConversionService`**:
    *   `var conversionResult = _conversionService.ConvertToOrder(teamCart, ...);`
    *   This service performs the complex mapping from `TeamCart` to a new `Order` aggregate.
5.  **Persist both aggregates**:
    *   `await _orderRepository.AddAsync(newOrder);`
    *   `await _teamCartRepository.UpdateAsync(updatedTeamCart);` // `teamCart` is now marked as `Converted`
6.  **Commit Transaction.**
7.  **Return** the new `OrderId`.
