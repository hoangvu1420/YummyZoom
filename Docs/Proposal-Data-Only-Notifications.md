# Proposal: Data-Only Notifications for Low-Value Events

## Goal
Enable low-value events to be sent as data-only FCM messages (no notification tray) while still allowing the app to receive and display them in-app (badges, in-app notifications, etc.).

## Current State Analysis

### Events Currently Sending Push Notifications

**High-Value (Critical Alerts) - Should remain Hybrid:**
- `cart_locked` - TeamCartLockedForPaymentEventHandler
- `cart_converted` - TeamCartConvertedEventHandler  
- `cart_expired` - TeamCartExpiredEventHandler

**Medium-Value (Important Updates) - Should be Data-Only:**
- `payment_succeeded` - OnlinePaymentSucceededEventHandler (Payer only)
- `payment_failed` - OnlinePaymentFailedEventHandler (Payer only)
- `ready_for_confirmation` - TeamCartReadyForConfirmationEventHandler (All members)
- `member_joined` - MemberJoinedEventHandler (Host only)

**Low-Value (Frequent Updates) - Should be Data-Only:**
- `tip_applied` - TipAppliedToTeamCartEventHandler (Members)
- `coupon_applied` - CouponAppliedToTeamCartEventHandler (Members)
- `coupon_removed` - CouponRemovedFromTeamCartEventHandler (Members)
- `payment_committed` - MemberCommittedToPaymentEventHandler (Payer only)
- `member_ready_changed` - SetMemberReadyCommand (Host only, when all ready)
- `item_added` - ItemAddedToTeamCartEventHandler (add this back)
- `item_removed` - ItemRemovedFromTeamCartEventHandler (add this back)

**Already Suppressed:**
- `item_quantity_updated` - ItemQuantityUpdatedInTeamCartEventHandler (already suppressed)

## Proposed Architecture

### Option 1: Add Notification Delivery Type Enum (Recommended)

**Pros:**
- Clean, explicit control
- Type-safe
- Easy to understand and maintain
- Single method signature

**Cons:**
- Requires updating all call sites

### Option 2: Add Separate Method for Data-Only

**Pros:**
- Clear separation of concerns
- No breaking changes to existing method
- Easy to understand intent

**Cons:**
- Code duplication
- Two methods to maintain

### Option 3: Auto-Detect Based on Event Type

**Pros:**
- No changes to call sites
- Centralized logic

**Cons:**
- Less explicit
- Harder to override per event
- Magic behavior

## Recommended Implementation: Option 1 + Helper Method

### Step 1: Add Notification Delivery Type Enum

```csharp
public enum NotificationDeliveryType
{
    /// <summary>
    /// Hybrid notification (notification + data) - shows in notification tray
    /// </summary>
    Hybrid = 0,
    
    /// <summary>
    /// Data-only notification - no notification tray, app handles silently
    /// </summary>
    DataOnly = 1
}
```

### Step 2: Update Interface

```csharp
public interface ITeamCartPushNotifier
{
    Task<Result> PushTeamCartDataAsync(
        TeamCartId teamCartId, 
        long version,
        TeamCartNotificationTarget target = TeamCartNotificationTarget.All,
        TeamCartNotificationContext? context = null,
        NotificationDeliveryType deliveryType = NotificationDeliveryType.Hybrid,
        CancellationToken cancellationToken = default);
}
```

### Step 3: Refactor Implementation

- Extract common logic (token collection, target resolution) into shared methods
- Add conditional logic to call `SendMulticastNotificationAsync` vs `SendMulticastDataAsync`
- For data-only, still generate title/body for logging but don't include in notification block

### Step 4: Add Helper Method to Determine Default Delivery Type

```csharp
private static NotificationDeliveryType GetDefaultDeliveryType(string? eventType)
{
    return eventType switch
    {
        // High-value: Hybrid (show in notification tray)
        "TeamCartLockedForPayment" => NotificationDeliveryType.Hybrid,
        "TeamCartConverted" => NotificationDeliveryType.Hybrid,
        "TeamCartExpired" => NotificationDeliveryType.Hybrid,
        
        // Medium-value: Could be hybrid or data-only (default to hybrid for now)
        "OnlinePaymentSucceeded" => NotificationDeliveryType.Hybrid,
        "OnlinePaymentFailed" => NotificationDeliveryType.Hybrid,
        "TeamCartReadyForConfirmation" => NotificationDeliveryType.Hybrid,
        "MemberJoined" => NotificationDeliveryType.Hybrid,
        
        // Low-value: Data-only (no notification tray)
        "TipApplied" => NotificationDeliveryType.DataOnly,
        "CouponApplied" => NotificationDeliveryType.DataOnly,
        "CouponRemoved" => NotificationDeliveryType.DataOnly,
        "MemberCommittedToPayment" => NotificationDeliveryType.DataOnly,
        "MemberReadyStatusChanged" => NotificationDeliveryType.DataOnly,
        
        _ => NotificationDeliveryType.Hybrid // Default to hybrid for safety
    };
}
```

### Step 5: Update Event Handlers

Update low-value event handlers to explicitly use `DataOnly`:

```csharp
// Example: TipAppliedToTeamCartEventHandler
var push = await _pushNotifier.PushTeamCartDataAsync(
    cartId, 
    vm.Version, 
    TeamCartNotificationTarget.Members,
    context,
    NotificationDeliveryType.DataOnly, // Explicitly set
    ct);
```

## Implementation Details

### Data-Only Payload Structure

For data-only messages, the payload will be:
```json
{
  "data": {
    "type": "teamcart",
    "teamCartId": "guid",
    "version": "15",
    "state": "Active",
    "click_action": "FLUTTER_NOTIFICATION_CLICK",
    "route": "/teamcart/guid",
    "actorId": "user-456",
    "event": "tip_applied"
  }
}
```

**Note:** No `notification` block, so OS won't display it in notification tray.

### Hybrid Payload Structure (Unchanged)

```json
{
  "notification": {
    "title": "Tip đã thêm",
    "body": "Chủ giỏ đã thêm tip 50,000 VND...",
    "sound": "default"
  },
  "data": {
    "type": "teamcart",
    "teamCartId": "guid",
    "version": "15",
    "state": "Active",
    "click_action": "FLUTTER_NOTIFICATION_CLICK",
    "route": "/teamcart/guid",
    "actorId": "user-456",
    "event": "tip_applied"
  }
}
```

## Code Changes Required

### Files to Modify:

1. **TeamCartNotificationTarget.cs** (or new file)
   - Add `NotificationDeliveryType` enum

2. **ITeamCartPushNotifier.cs**
   - Add `deliveryType` parameter to `PushTeamCartDataAsync`

3. **TeamCartPushNotifier.cs**
   - Refactor `PushTeamCartDataAsync` to support both delivery types
   - Add `GetDefaultDeliveryType` helper method
   - Extract common logic into shared methods

4. **Event Handlers** (Low-value events):
   - `TipAppliedToTeamCartEventHandler.cs`
   - `CouponAppliedToTeamCartEventHandler.cs`
   - `CouponRemovedFromTeamCartEventHandler.cs`
   - `MemberCommittedToPaymentEventHandler.cs`
   - `SetMemberReadyCommand.cs`

### Refactoring Strategy

1. Extract token collection logic:
   ```csharp
   private async Task<List<string>> CollectValidTokensAsync(
       TeamCartViewModel vm,
       TeamCartNotificationTarget target,
       Guid? actorUserId,
       CancellationToken cancellationToken)
   ```

2. Extract data payload creation:
   ```csharp
   private static Dictionary<string, string> CreateDataPayload(
       TeamCartId teamCartId,
       long version,
       TeamCartViewModel vm,
       TeamCartNotificationContext? context)
   ```

3. Main method becomes:
   ```csharp
   public async Task<Result> PushTeamCartDataAsync(...)
   {
       var tokens = await CollectValidTokensAsync(...);
       var data = CreateDataPayload(...);
       
       if (deliveryType == NotificationDeliveryType.DataOnly)
       {
           return await _fcm.SendMulticastDataAsync(tokens, data);
       }
       else
       {
           var (title, body) = GenerateContextualMessage(...);
           return await _fcm.SendMulticastNotificationAsync(tokens, title, body, data);
       }
   }
   ```

## Benefits

1. **Reduced Notification Spam:** Low-value events don't clutter notification tray
2. **Better UX:** Users see important notifications, but app still receives all updates
3. **Flexible:** Easy to change delivery type per event
4. **Backward Compatible:** Default to Hybrid maintains existing behavior
5. **Clean Architecture:** Single method with clear parameter

## Migration Path

1. **Phase 1:** Add enum and parameter (default to Hybrid) - no behavior change
2. **Phase 2:** Update low-value event handlers to use DataOnly
3. **Phase 3:** Test and verify
4. **Phase 4:** Consider making medium-value events configurable

## Testing Considerations

- Verify data-only messages don't appear in notification tray
- Verify app receives data-only messages when running
- Verify app receives data-only messages when killed (FCM handles this)
- Test all event types with both delivery types
- Verify backward compatibility (default Hybrid)

## Frontend Impact

Frontend needs to handle data-only messages:
- Check if message has `notification` block
- If not, handle silently (update UI, badges, etc.)
- If yes, show notification tray + handle data

No breaking changes - frontend can detect presence of notification block.

