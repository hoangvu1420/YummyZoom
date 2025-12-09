# TeamCart 403 Forbidden Error - Analysis Report

**Date:** 2025-01-27  
**Issue:** Users receiving 403 Forbidden when attempting to add items to TeamCart after successfully joining  
**Status:** Root Cause Identified

---

## Executive Summary

Users are experiencing a 403 Forbidden error when attempting to add items to a TeamCart, even though they have successfully joined the TeamCart. The issue is caused by a **mismatch between database state and authorization claims**: users are added as members in the database, but their JWT tokens do not contain the required permission claims for TeamCart membership.

---

## Root Cause Analysis

### Problem Flow

1. **User Joins TeamCart Successfully**
   - Client calls `POST /api/v1/team-carts/{id}/join`
   - `JoinTeamCartCommandHandler` executes and adds the user as a member to the TeamCart in the database
   - Response: 204 No Content (success)

2. **User Attempts to Add Item**
   - Client calls `POST /api/v1/team-carts/{id}/items`
   - `AddItemToTeamCartCommand` requires `[Authorize(Policy = Policies.MustBeTeamCartMember)]`
   - Authorization pipeline checks for permission claim: `TeamCartMember:{teamCartId}`
   - **Claim is missing from user's JWT token**
   - Result: 403 Forbidden

### Technical Details

#### Authorization Architecture

The authorization system uses a **claims-based approach** where permission claims are embedded in the JWT token:

```csharp
// Required claim format for TeamCart membership
permission: "TeamCartMember:{teamCartId}"
```

#### Claims Principal Factory

The `YummyZoomClaimsPrincipalFactory` (```28:66:src/Infrastructure/Identity/YummyZoomClaimsPrincipalFactory.cs```) currently adds claims for:
- ✅ Restaurant roles (Owner, Staff)
- ✅ User self-ownership
- ✅ Admin permissions
- ✅ Active orders
- ❌ **TeamCart memberships (MISSING)**

#### Authorization Handler

The `PermissionAuthorizationHandler` (```101:115:src/Application/Common/Authorization/PermissionAuthorizationHandler.cs```) checks for:
1. Exact permission claim: `TeamCartMember:{teamCartId}`
2. Fallback: Host permission can satisfy member requirement

Since neither exists in the token after joining, authorization fails.

---

## Evidence from Codebase

### 1. Join Command Handler
```csharp
// src/Application/TeamCarts/Commands/JoinTeamCart/JoinTeamCartCommandHandler.cs
// Adds member to database but does NOT update claims
var addMemberResult = cart.AddMember(guestUserId, request.GuestName, MemberRole.Guest);
await _teamCartRepository.UpdateAsync(cart, cancellationToken);
```

### 2. Add Item Command Authorization
```csharp
// src/Application/TeamCarts/Commands/AddItemToTeamCart/AddItemToTeamCartCommand.cs
[Authorize(Policy = Policies.MustBeTeamCartMember)]
public sealed record AddItemToTeamCartCommand(...)
```

### 3. Claims Factory Missing TeamCart Logic
```csharp
// src/Infrastructure/Identity/YummyZoomClaimsPrincipalFactory.cs
// Lines 58-63: Orders are handled, but TeamCarts are not
var activeOrderIds = await _orderRepository.GetActiveOrderIdsForCustomerAsync(user.Id);
foreach (var orderId in activeOrderIds)
{
    identity.AddClaim(new Claim("permission", $"{Roles.OrderOwner}:{orderId}"));
}
// ❌ No equivalent code for TeamCart memberships
```

### 4. Functional Tests Work Around This
```csharp
// tests/Application.FunctionalTests/Authorization/TeamCartRoleTestHelper.cs
// Tests manually add claims because they know the factory doesn't
TestAuthenticationService.AddPermissionClaim(Roles.TeamCartMember, teamCartId.ToString());
```

---

## Impact Assessment

### Affected Operations
All TeamCart member operations that require `MustBeTeamCartMember` policy:
- ❌ `AddItemToTeamCartCommand` - **Currently failing**
- ❌ `UpdateTeamCartItemQuantityCommand` - Will fail
- ❌ `RemoveItemFromTeamCartCommand` - Will fail
- ❌ `SetMemberReadyCommand` - Will fail
- ❌ `CommitToCodPaymentCommand` - Will fail
- ❌ `InitiateMemberOnlinePaymentCommand` - Will fail

### Workaround Status
- **Host operations work** because hosts get claims when creating the TeamCart (via `CreateTeamCartCommand`)
- **Guest operations fail** because guests don't get claims when joining

---

## Proposed Solutions

### Solution 1: Add TeamCart Claims to Claims Principal Factory (RECOMMENDED)

**Approach:** Modify `YummyZoomClaimsPrincipalFactory` to query TeamCart memberships and add claims, similar to how active orders are handled.

**Pros:**
- ✅ Consistent with existing architecture (orders pattern)
- ✅ No client-side changes required
- ✅ Claims refresh automatically on token refresh
- ✅ Works for all TeamCart operations

**Cons:**
- ⚠️ Requires database query on every token generation
- ⚠️ Claims may be stale until token refresh

**Implementation:**
1. Add `ITeamCartRepository` dependency to `YummyZoomClaimsPrincipalFactory`
2. Query active TeamCart memberships for the user
3. Add `TeamCartMember:{teamCartId}` claims for guests
4. Add `TeamCartHost:{teamCartId}` claims for hosts

**Code Location:** `src/Infrastructure/Identity/YummyZoomClaimsPrincipalFactory.cs`

---

### Solution 2: Database-Backed Authorization Handler

**Approach:** Create a new authorization handler that queries the database directly for TeamCart membership instead of relying on claims.

**Pros:**
- ✅ Always up-to-date (no stale claims)
- ✅ No token refresh required

**Cons:**
- ❌ Database query on every authorization check (performance impact)
- ❌ Inconsistent with existing claims-based pattern
- ⚠️ Requires new handler implementation

**Implementation:**
1. Create `TeamCartMembershipAuthorizationHandler`
2. Query `TeamCartMembers` table during authorization
3. Register handler in DI container

---

### Solution 3: Client-Side Token Refresh

**Approach:** Client refreshes the authentication token immediately after joining a TeamCart.

**Pros:**
- ✅ Quick fix (no server changes)
- ✅ Works with existing claims factory once Solution 1 is implemented

**Cons:**
- ❌ Requires client-side changes
- ❌ Additional API call overhead
- ❌ Not a permanent solution (still needs Solution 1)

---

## Recommended Implementation Plan

### Phase 1: Immediate Fix (Solution 1)

1. **Update Claims Principal Factory**
   - Add `ITeamCartRepository` dependency
   - Query active TeamCart memberships
   - Add appropriate permission claims

2. **Testing**
   - Verify claims are added correctly
   - Test authorization flow end-to-end
   - Ensure no performance regression

3. **Deployment**
   - Deploy to staging environment
   - Monitor token generation performance
   - Verify claims are present in tokens

### Phase 2: Client Communication

**Message to Frontend Team:**

> **Action Required:** After implementing the server-side fix, users will need to refresh their authentication token after joining a TeamCart to receive the updated claims. This can be done by:
> 1. Calling your token refresh endpoint after a successful join
> 2. Or waiting for the next automatic token refresh cycle
>
> **Temporary Workaround:** Until the fix is deployed, users can work around this by logging out and logging back in after joining a TeamCart.

### Phase 3: Long-Term Optimization (Optional)

Consider implementing Solution 2 as a fallback for cases where claims might be stale, or implement a hybrid approach that checks claims first and falls back to database if needed.

---

## Testing Recommendations

### Unit Tests
- Test `YummyZoomClaimsPrincipalFactory` with TeamCart memberships
- Verify claims are added for both hosts and guests
- Test edge cases (expired carts, multiple memberships)

### Integration Tests
- End-to-end flow: Join → Add Item (should succeed)
- Verify claims are present in generated tokens
- Test token refresh after joining

### Functional Tests
- Update existing tests to verify claims are generated
- Add test for guest user adding items after joining
- Test concurrent join + add item scenarios

---

## Related Files

### Core Authorization
- `src/Application/Common/Authorization/PermissionAuthorizationHandler.cs`
- `src/Application/Common/Behaviours/AuthorizationBehaviour.cs`
- `src/Infrastructure/Identity/YummyZoomClaimsPrincipalFactory.cs`

### TeamCart Commands
- `src/Application/TeamCarts/Commands/JoinTeamCart/JoinTeamCartCommandHandler.cs`
- `src/Application/TeamCarts/Commands/AddItemToTeamCart/AddItemToTeamCartCommand.cs`

### Domain Logic
- `src/Domain/TeamCartAggregate/TeamCart.cs` (AddMember, AddItem methods)

### Tests
- `tests/Application.FunctionalTests/Features/TeamCarts/Commands/AddItemToTeamCart/AddItemToTeamCartTests.cs`
- `tests/Application.FunctionalTests/Authorization/TeamCartRoleTestHelper.cs`

---

## Next Steps

1. ✅ **Analysis Complete** - Root cause identified
2. ⏳ **Implementation** - Add TeamCart claims to claims factory
3. ⏳ **Testing** - Verify fix resolves the issue
4. ⏳ **Deployment** - Deploy to production
5. ⏳ **Client Communication** - Share findings and recommendations

---

## Questions for Frontend Team

1. **Token Refresh Strategy:** How does your app currently handle token refresh? Is it automatic or manual?
2. **Error Handling:** What user-facing error message is shown when receiving 403?
3. **Workflow:** After a user joins a TeamCart, what is the next action they typically take? (This helps prioritize the fix)
4. **Testing:** Can you reproduce this issue consistently? If so, what are the exact steps?

---

## Conclusion

The 403 Forbidden error is caused by missing permission claims in JWT tokens after users join TeamCarts. The recommended solution is to add TeamCart membership claims to the claims principal factory, following the existing pattern used for active orders. This will ensure users have the necessary permissions immediately after joining, without requiring client-side changes.

**Estimated Fix Time:** 2-4 hours (implementation + testing)  
**Risk Level:** Low (follows existing patterns)  
**Breaking Changes:** None

