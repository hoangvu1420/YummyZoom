# TeamCart 403 Forbidden Issue - Frontend Team Summary

**Date:** 2025-01-27  
**Status:** ✅ Server-Side Fix Implemented  
**Priority:** High - Client Action Required

---

## Issue Summary

Users were receiving **403 Forbidden** errors when attempting to add items to a TeamCart after successfully joining, even though:
- ✅ User is authenticated
- ✅ User has successfully joined the TeamCart (database confirms membership)
- ❌ Authorization check fails because JWT token lacks required permission claim

---

## Server-Side Fix (✅ Completed)

### What Was Changed

The server now automatically includes TeamCart membership claims in JWT tokens when they are **generated or refreshed**. This follows the same pattern used for active orders.

**Changes Made:**
1. Added `GetActiveTeamCartMembershipsAsync` method to query active TeamCart memberships
2. Updated `YummyZoomClaimsPrincipalFactory` to add TeamCart permission claims:
   - `TeamCartHost:{teamCartId}` for hosts
   - `TeamCartMember:{teamCartId}` for guests
3. Claims are added for TeamCarts in `Open` or `Locked` status that haven't expired

### How It Works

When a user's authentication token is generated or refreshed, the server:
1. Queries the database for active TeamCart memberships
2. Adds appropriate permission claims to the token
3. Token now contains the required claims for authorization

---

## ⚠️ Critical: Token Refresh Requirement

**IMPORTANT:** The fix only works when tokens are **refreshed after joining** a TeamCart.

### The Problem

If a user:
1. Logs in → Gets token (no TeamCart claims yet)
2. Joins TeamCart → Member added to database ✅
3. Tries to add item with **old token** → ❌ **403 Forbidden**

**Why?** The old token was generated before joining, so it doesn't contain TeamCart membership claims.

### The Solution

Users must **refresh their authentication token** after joining a TeamCart to receive the updated claims.

---

## Client App Implementation Recommendations

### Option 1: Automatic Token Refresh After Join (✅ Recommended)

Refresh the token immediately after a successful join operation:

```typescript
async function joinTeamCart(teamCartId: string, shareToken: string, guestName: string) {
  // 1. Join the TeamCart
  const joinResponse = await fetch(`/api/v1/team-carts/${teamCartId}/join`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${currentToken}`
    },
    body: JSON.stringify({ shareToken, guestName })
  });

  if (!joinResponse.ok) {
    throw new Error('Failed to join TeamCart');
  }

  // 2. Refresh token to get updated claims
  await refreshAuthToken(); // Your token refresh implementation

  return joinResponse;
}
```

**Benefits:**
- ✅ Seamless user experience
- ✅ Claims available immediately
- ✅ No additional user action required

### Option 2: Retry with Token Refresh on 403

If you receive a 403 when adding items, refresh and retry:

```typescript
async function addItemToTeamCart(
  teamCartId: string, 
  menuItemId: string, 
  quantity: number
) {
  let response = await fetch(`/api/v1/team-carts/${teamCartId}/items`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${currentToken}`
    },
    body: JSON.stringify({ menuItemId, quantity })
  });

  // If 403, refresh token and retry once
  if (response.status === 403) {
    await refreshAuthToken();
    
    response = await fetch(`/api/v1/team-carts/${teamCartId}/items`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${currentToken}` // Updated token
      },
      body: JSON.stringify({ menuItemId, quantity })
    });
  }

  return response;
}
```

**Benefits:**
- ✅ Handles edge cases
- ✅ Works even if token refresh after join was missed

**Drawbacks:**
- ⚠️ Additional API call on failure
- ⚠️ Slightly slower user experience

### Option 3: Periodic Token Refresh

If your app already has automatic token refresh (e.g., every 15 minutes), this will work, but users may experience delays:

- User joins TeamCart → Claims added to database ✅
- User tries to add item → 403 if token not refreshed yet ❌
- Token refreshes automatically → Claims now available ✅
- User retries → Success ✅

**Recommendation:** Combine with Option 1 for best experience.

---

## Affected Endpoints

All endpoints requiring `MustBeTeamCartMember` policy will fail without refreshed token:

- ❌ `POST /api/v1/team-carts/{id}/items` - Add item
- ❌ `PUT /api/v1/team-carts/{id}/items/{itemId}` - Update item quantity
- ❌ `DELETE /api/v1/team-carts/{id}/items/{itemId}` - Remove item
- ❌ `POST /api/v1/team-carts/{id}/set-ready` - Set member ready
- ❌ `POST /api/v1/team-carts/{id}/commit-cod` - Commit to COD payment
- ❌ `POST /api/v1/team-carts/{id}/initiate-online-payment` - Initiate online payment

**Note:** Host operations work because hosts receive claims when creating the TeamCart.

---

## Testing Checklist

Please verify the following scenarios:

### ✅ Test Case 1: Join Then Add Item (With Token Refresh)
1. User logs in → Gets token
2. User joins TeamCart → Success
3. **Token is refreshed** → Claims updated
4. User adds item → ✅ Should succeed

### ✅ Test Case 2: Join Then Add Item (Without Token Refresh)
1. User logs in → Gets token
2. User joins TeamCart → Success
3. **Token is NOT refreshed** → Claims not updated
4. User adds item → ❌ Should get 403 Forbidden
5. Token refresh → Claims updated
6. User retries adding item → ✅ Should succeed

### ✅ Test Case 3: Host Creates and Adds Items
1. Host creates TeamCart → Gets token with `TeamCartHost` claim
2. Host adds items → ✅ Should succeed (no refresh needed)

### ✅ Test Case 4: Multiple TeamCarts
1. User joins TeamCart A → Refresh token
2. User joins TeamCart B → Refresh token
3. User adds item to TeamCart A → ✅ Should succeed
4. User adds item to TeamCart B → ✅ Should succeed

---

## Implementation Priority

1. **High Priority:** Implement automatic token refresh after join (Option 1)
2. **Medium Priority:** Add retry logic with token refresh on 403 (Option 2)
3. **Low Priority:** Update error messages to guide users if 403 occurs

---

## Error Handling Recommendations

### User-Friendly Error Messages

If a 403 occurs, consider showing:

```
"Unable to add item. Please wait a moment and try again."
```

Then automatically:
1. Refresh the token
2. Retry the operation
3. Show success message if retry succeeds

### Logging

Log when 403 errors occur for TeamCart operations to help identify if token refresh is working correctly:

```typescript
if (response.status === 403 && isTeamCartOperation(url)) {
  console.warn('TeamCart 403 error - token may need refresh', {
    endpoint: url,
    userId: currentUserId,
    teamCartId: extractTeamCartId(url)
  });
}
```

---

## Questions?

If you have questions about:
- Token refresh implementation
- Error handling strategies
- Testing scenarios
- Performance considerations

Please contact the backend team lead.

---

## Technical Details (For Reference)

### Required Claims Format

```
permission: "TeamCartMember:{teamCartId}"  // For guests
permission: "TeamCartHost:{teamCartId}"   // For hosts
```

### Token Refresh Endpoint

Use your existing token refresh endpoint. The server will automatically include TeamCart claims when generating the new token.

### Active TeamCart Criteria

Claims are added for TeamCarts that are:
- Status: `Open` or `Locked`
- Not expired (`ExpiresAt > now`)
- User is a member (in `TeamCartMembers` table)

---

**Status:** Server fix deployed. Client implementation recommended for optimal user experience.

