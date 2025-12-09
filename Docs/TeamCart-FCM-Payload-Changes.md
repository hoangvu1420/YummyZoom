# Team Cart FCM Notification Payload Changes

**Date:** 2024  
**Status:** ✅ Implemented  
**Impact:** Breaking Change - Requires Frontend Update

## Summary

The Team Cart FCM notification payload has been updated from **data-only** messages to **hybrid** messages (notification + data) to ensure 100% delivery rates, even when the app is killed.

## What Changed

### Before (Data-Only)
```json
{
  "type": "teamcart",
  "teamCartId": "guid",
  "version": "15",
  "state": "Active",
  "title": "Món mới",
  "body": "Tùng đã thêm Pizza Hải Sản (x1)",
  "message": "Tùng đã thêm Pizza Hải Sản (x1)",
  "route": "/team-carts/guid"
}
```

### After (Hybrid - Notification + Data)
```json
{
  "notification": {
    "title": "Món mới",
    "body": "Tùng đã thêm Pizza Hải Sản (x1)",
    "sound": "default"
  },
  "data": {
    "type": "teamcart",
    "teamCartId": "guid-uuid-123",
    "version": "15",
    "state": "Active",
    "click_action": "FLUTTER_NOTIFICATION_CLICK",
    "route": "/teamcart/guid-uuid-123",
    "actorId": "user-456",
    "event": "item_added"
  }
}
```

## Key Changes

### 1. Payload Structure
- **Added:** `notification` block (handled by OS for display)
- **Removed from data:** `title`, `body`, `message` (now in notification block)
- **Added to data:** `click_action`, `actorId`, `event`
- **Changed:** Route format from `/team-carts/` to `/teamcart/`

### 2. New Data Fields

| Field | Type | Description |
|-------|------|-------------|
| `click_action` | string | Always `"FLUTTER_NOTIFICATION_CLICK"` for Flutter deep linking |
| `actorId` | string | User ID of the person who performed the action (empty string if not applicable) |
| `event` | string | Event type enum (see mapping below) |

### 3. Event Type Mapping

The `event` field uses snake_case enum values:

| Backend Event | Frontend Enum |
|--------------|--------------|
| MemberJoined | `member_joined` |
| ItemAdded | `item_added` |
| ItemRemoved | `item_removed` |
| ItemQuantityUpdated | `item_quantity_updated` |
| TeamCartLockedForPayment | `cart_locked` |
| TipApplied | `tip_applied` |
| CouponApplied | `coupon_applied` |
| CouponRemoved | `coupon_removed` |
| MemberCommittedToPayment | `payment_committed` |
| OnlinePaymentSucceeded | `payment_succeeded` |
| OnlinePaymentFailed | `payment_failed` |
| TeamCartReadyForConfirmation | `ready_for_confirmation` |
| TeamCartConverted | `cart_converted` |
| TeamCartExpired | `cart_expired` |
| MemberReadyStatusChanged | `member_ready_changed` |
| (fallback) | `state_changed` |

## Migration Guide

### 1. Update FCM Message Handler

**Old:**
```dart
if (data['type'] == 'teamcart') {
  final title = data['title'];
  final body = data['body'];
  // Handle notification display...
}
```

**New:**
```dart
// Handle notification block (OS displays automatically)
final notification = message.notification;
if (notification != null) {
  // OS handles display, but you can customize if needed
}

// Handle data block for app logic
final data = message.data;
if (data['type'] == 'teamcart') {
  final teamCartId = data['teamCartId'];
  final event = data['event'];
  final actorId = data['actorId'];
  final route = data['route'];
  
  // Use event enum for specific UI handling
  // Use actorId to ignore own actions if needed
  // Navigate using route
}
```

### 2. Update Deep Link Handling

- **Old route:** `/team-carts/{teamCartId}`
- **New route:** `/teamcart/{teamCartId}`

Update your route configuration accordingly.

### 3. Actor Exclusion Logic

You can now use `actorId` to ignore notifications for the current user's own actions:

```dart
final currentUserId = getCurrentUserId();
if (data['actorId'] == currentUserId) {
  // Skip notification or handle silently
  return;
}
```

## Benefits

1. **100% Delivery:** OS handles notification display even when app is killed
2. **Better UX:** Users see notifications immediately without app running
3. **Cleaner Data:** Removed duplicate title/body/message fields
4. **Event Enum:** Structured event types for easier UI handling
5. **Actor Tracking:** Know who performed the action for better personalization

## Testing Checklist

- [ ] Verify notifications display when app is killed
- [ ] Verify notifications display when app is in background
- [ ] Verify deep linking works with new route format
- [ ] Test all 15 event types
- [ ] Verify actor exclusion logic (don't notify user for own actions)
- [ ] Test on both Android and iOS

## Questions?

Contact the backend team if you need clarification on any changes.

