Comparing `TeamCartPushNotifier` with `OrderPushNotifier` to identify differences. Reading `OrderPushNotifier`:


[3 tools called]


Reviewing a few more order event handlers to confirm the pattern:


[4 tools called]


## Comparison: TeamCartPushNotifier vs OrderPushNotifier

### 1. Invocation pattern and architecture

#### OrderPushNotifier — explicit dependency injection
- Pattern: Direct dependency injection in event handlers
- Location: Called explicitly from each order event handler
- Flow:
```
Event Handler → IOrderRealtimeNotifier (SignalR) → [Separate call] → IOrderPushNotifier (FCM)
```
- Separation: SignalR and FCM are independent calls in the handler

#### TeamCartPushNotifier — indirect scoped resolution
- Pattern: Resolved via scoped service provider inside SignalR notifier
- Location: Called from `SignalRTeamCartRealtimeNotifier.SendAsync()`
- Flow:
```
Event Handler → ITeamCartRealtimeNotifier → SignalR → [Inside SignalR] → ITeamCartPushNotifier (FCM)
```
- Coupling: FCM push is triggered inside the SignalR notifier

### 2. Method signature and parameters

#### OrderPushNotifier
```csharp
Task<Result> PushOrderDataAsync(
    Guid orderId, 
    Guid customerUserId,  // Explicit single user
    long version,          // Explicit version
    CancellationToken cancellationToken = default
)
```
- Parameters: Explicit `orderId`, `customerUserId`, `version`
- User resolution: Single user passed as parameter
- Version: Explicitly passed

#### TeamCartPushNotifier
```csharp
Task<Result> PushTeamCartDataAsync(
    TeamCartId teamCartId,  // Only cart ID
    CancellationToken cancellationToken = default
)
```
- Parameters: Only `teamCartId`
- User resolution: Extracted from VM (all members)
- Version: Retrieved from VM

### 3. Data source and loading strategy

#### OrderPushNotifier
```csharp
// Loads full aggregate from repository
var order = await _orderRepository.GetByIdAsync(OrderId.Create(orderId), cancellationToken);
```
- Source: Domain aggregate (EF Core repository)
- Loading: Full aggregate load
- Enrichment: Uses aggregate properties (OrderNumber, Status, etc.)
- Payload: Includes `message` field with order number

#### TeamCartPushNotifier
```csharp
// Loads view model from Redis
var vm = await _store.GetVmAsync(teamCartId, cancellationToken);
```
- Source: Redis view model (read-optimized)
- Loading: VM from cache
- Enrichment: Uses VM properties (Status, Version, Members)
- Payload: No order number equivalent

### 4. Target user resolution

#### OrderPushNotifier
```csharp
// Single user - passed as parameter
var tokens = await _userDeviceSessionRepository.GetActiveFcmTokensByUserIdAsync(
    customerUserId, cancellationToken);
```
- Target: Single user (customer)
- Resolution: Direct lookup for one user
- Pattern: One-to-one (order → customer)

#### TeamCartPushNotifier
```csharp
// Multiple users - extracted from VM
var userIds = vm.Members.Select(m => m.UserId).Distinct().ToList();
foreach (var uid in userIds)
{
    var list = await _userDeviceSessions.GetActiveFcmTokensByUserIdAsync(uid, cancellationToken);
    foreach (var t in list) tokens.Add(t);
}
```
- Target: Multiple users (all members)
- Resolution: Iterates through all members
- Pattern: One-to-many (teamcart → members)

### 5. Error handling and failure behavior

#### OrderPushNotifier
```csharp
var push = await _orderPushNotifier.PushOrderDataAsync(...);
if (push.IsFailure)
{
    throw new InvalidOperationException(push.Error.Description);
}
```
- Behavior: Failures throw exceptions
- Impact: Fails the entire event handler
- Retry: Outbox retry mechanism
- Consistency: SignalR and FCM succeed or fail together

#### TeamCartPushNotifier
```csharp
var pushResult = await push.PushTeamCartDataAsync(cartId, ct);
if (pushResult.IsFailure)
{
    _logger.LogWarning("Failed to send TeamCart FCM data push: {Error}", pushResult.Error);
    // Does NOT throw - SignalR already succeeded
}
```
- Behavior: Failures logged as warnings
- Impact: Does not fail SignalR notification
- Retry: No retry (SignalR already completed)
- Consistency: SignalR can succeed while FCM fails

### 6. Graceful degradation

#### OrderPushNotifier
```csharp
if (tokens.Count == 0)
{
    _logger.LogWarning("No active device tokens; skipping Order FCM push...");
    return Result.Success();
}
```
- Log level: Warning
- Behavior: Returns success (no-op)

#### TeamCartPushNotifier
```csharp
if (vm is null)
{
    _logger.LogDebug("TeamCart VM not found; skipping FCM push...");
    return Result.Success();
}
if (userIds.Count == 0)
{
    _logger.LogDebug("No members in TeamCart VM; skipping FCM push...");
    return Result.Success();
}
if (tokens.Count == 0)
{
    _logger.LogDebug("No active device tokens; skipping FCM push...");
    return Result.Success();
}
```
- Log level: Debug (more granular checks)
- Behavior: Returns success (no-op) with multiple early exits

### 7. Payload construction

#### OrderPushNotifier
```csharp
var message = !string.IsNullOrWhiteSpace(order.OrderNumber)
    ? $"Đơn hàng #{order.OrderNumber} {TrimTrailingPeriod(body)}"
    : body;

var data = new Dictionary<string, string>
{
    ["type"] = "order",
    ["orderId"] = orderId.ToString(),
    ["version"] = version.ToString(),
    ["status"] = status,
    ["title"] = title,
    ["body"] = body,
    ["message"] = message,  // Enhanced message with order number
    ["route"] = route
};
```
- Fields: 8 fields including enhanced `message`
- Enrichment: Includes order number in message
- Helper: `TrimTrailingPeriod()` utility

#### TeamCartPushNotifier
```csharp
var data = new Dictionary<string, string>
{
    ["type"] = "teamcart",
    ["teamCartId"] = teamCartId.Value.ToString(),
    ["version"] = vm.Version.ToString(),
    ["state"] = state,
    ["title"] = title,
    ["body"] = body,
    ["route"] = route
};
```
- Fields: 7 fields (no enhanced message)
- Enrichment: Basic localization only
- Helper: No message enhancement utilities

### 8. Dependency injection and lifecycle

#### OrderPushNotifier
- Registration: Singleton (typical for infrastructure services)
- Dependencies: Injected via constructor
- Scope: Same as event handler
- Performance: No scope creation overhead

#### TeamCartPushNotifier
- Registration: Scoped (resolved via `IServiceProvider.CreateScope()`)
- Dependencies: Resolved from scoped service provider
- Scope: New scope created for each notification
- Performance: Overhead of scope creation per notification

### 9. Transaction and consistency

#### OrderPushNotifier
- Context: Called within event handler transaction
- Consistency: FCM failure causes handler failure → transaction rollback
- Atomicity: SignalR and FCM succeed or fail together

#### TeamCartPushNotifier
- Context: Called after SignalR (outside transaction)
- Consistency: FCM failure does not affect SignalR
- Atomicity: SignalR and FCM are independent

### 10. Summary table

| Aspect | OrderPushNotifier | TeamCartPushNotifier |
|--------|------------------|---------------------|
| Invocation | Explicit DI in handlers | Indirect via SignalR notifier |
| Parameters | `orderId`, `userId`, `version` | `teamCartId` only |
| Data Source | Domain aggregate (EF) | Redis VM (cache) |
| Target Users | Single (customer) | Multiple (all members) |
| Error Handling | Throws exception | Logs warning |
| Failure Impact | Fails handler | Does not fail SignalR |
| Logging (no tokens) | Warning | Debug |
| Payload Fields | 8 (with message) | 7 (basic) |
| Scope Management | Constructor DI | Scoped resolution |
| Transaction Safety | Within handler tx | Outside transaction |
| Retry Behavior | Outbox retry | No retry |

### 11. Architectural implications

#### OrderPushNotifier advantages
1. Explicit control: Handlers control when FCM is called
2. Transaction safety: FCM failures roll back the transaction
3. Clear separation: SignalR and FCM are independent
4. Better error handling: Failures propagate correctly
5. No scope overhead: Direct dependency injection

#### TeamCartPushNotifier advantages
1. Less boilerplate: Handlers don't need to inject push notifier
2. Automatic FCM: Every SignalR notification triggers FCM
3. Decoupled: Handlers don't know about FCM
4. Resilient: FCM failures don't break SignalR

#### TeamCartPushNotifier disadvantages
1. Hidden dependency: FCM call is implicit
2. No transaction safety: FCM runs outside transaction
3. Scope overhead: Creates new scope per notification
4. Error masking: FCM failures are logged but not propagated
5. Tight coupling: FCM is coupled to SignalR implementation

### 12. Recommendations

1. Align TeamCartPushNotifier with OrderPushNotifier pattern:
   - Inject `ITeamCartPushNotifier` directly in event handlers
   - Call FCM push explicitly alongside SignalR
   - Throw exceptions on FCM failure for consistency

2. If keeping the current pattern:
   - Document that FCM is triggered automatically
   - Consider making FCM failures non-blocking but trackable
   - Remove scope creation overhead (use singleton if possible)

3. Consider hybrid approach:
   - Keep automatic FCM for general updates
   - Add explicit FCM calls for critical events (conversion, expiration)

The OrderPushNotifier pattern is more explicit, testable, and transaction-safe. The TeamCartPushNotifier pattern reduces boilerplate but hides dependencies and reduces error visibility.