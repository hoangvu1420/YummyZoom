# TeamCart Simulation Manual Control Mode — Detailed Design & Plan

Status: Proposed (Dev/Test only)

Owner: Web/API

Date: 2025-12-12

## Goals

- Provide fine-grained manual control over TeamCart simulation flow for development and testing
- Allow developers to control when key steps execute (members join, items added, cart locked, payments committed)
- Maintain backward compatibility with existing automatic simulation mode
- Enable realistic testing scenarios where developers can pause and inspect state between steps
- Support automatic sub-sequences (e.g., members join one-by-one with delays after trigger)

## Non-Goals

- No production exposure (dev/test environments only)
- No changes to domain logic or business rules
- No new status polling endpoints (existing TeamCart queries sufficient)
- No individual member control (control all members together per phase)

## Current Implementation Analysis

### Existing Automatic Flow

The current `TeamCartFlowSimulator` runs entirely automatically:

1. **SimulateFullFlowAsync**: Creates cart, then automatically executes:
   - Members join (all at once, no delays between)
   - All members add items (sequentially, no delays between items)
   - All members mark ready (simultaneously)
   - Host locks cart (automatic)
   - All members commit payments (simultaneously)
   - Host converts to order (automatic)

2. **Timing Control**: Only configurable delays between major phases:
   - `HostCreateToGuestJoinMs`
   - `GuestJoinToAddItemsMs`
   - `AddItemsToMemberReadyMs`
   - `AllReadyToLockMs`
   - `LockToMemberPaymentMs`
   - `PaymentToConvertMs`

3. **Limitations**:
   - No control over when steps execute
   - No pauses between individual member actions within a phase
   - No ability to inspect state between steps
   - Background execution makes it difficult to debug

## Proposed Changes

### Mode-Based Control

Add a `mode` parameter to simulation requests:
- `"automatic"` (default): Works as current implementation (backward compatible)
- `"manual"`: Creates cart and initializes simulation state, then waits for step-by-step commands

### Manual Control Endpoints

Provide REST endpoints to trigger each major phase:
1. Initialize simulation (create cart, set up state)
2. Trigger members to join (automatic one-by-one with delays)
3. Trigger item addition phase (automatic with delays between items)
4. Trigger mark-ready phase
5. Trigger lock cart phase
6. Trigger payment phase (automatic one-by-one with delays)
7. Trigger convert to order phase

### Automatic Sub-Sequences

Within manual mode, certain phases execute automatically after trigger:
- **Members Join**: After trigger, members join one-by-one with configurable delay between each
- **Items Addition**: After trigger, each member adds items automatically with delay between each item
- **Payments**: After trigger, members commit payments one-by-one with delay between each

### State Management

Track simulation state in-memory:
- Current step/phase
- Completed steps
- Available next actions
- Member actions status

## API Design

### 1. Initialize Simulation (Enhanced)

**`POST /dev/team-carts/simulate-full-flow`**

Enhanced request body with `mode` parameter:

```json
{
  "hostPhone": "+84901234560",
  "memberPhones": ["+84901234561", "+84901234562"],
  "mode": "manual",  // NEW: "automatic" (default) or "manual"
  "scenario": "happyPath",
  "restaurantId": "guid",
  "delaysMs": {
    "hostCreateToGuestJoinMs": 2000,
    "guestJoinToAddItemsMs": 2000,
    "memberJoinDelayMs": 1000,        // NEW: delay between each member joining
    "itemAdditionDelayMs": 1500,      // NEW: delay between each item addition
    "addItemsToMemberReadyMs": 3000,
    "allReadyToLockMs": 15000,
    "lockToMemberPaymentMs": 5000,
    "memberPaymentDelayMs": 2000,     // NEW: delay between each member payment
    "paymentToConvertMs": 1500
  }
}
```

**Response** (same for both modes):
```json
{
  "runId": "guid",
  "teamCartId": "guid",
  "shareToken": "token",
  "scenario": "happyPath",
  "mode": "manual",
  "status": "Initialized",
  "startedAtUtc": "2025-12-12T10:30:00Z",
  "currentStep": "WaitingForMembersJoin",
  "simulatedMembers": ["+84901234561", "+84901234562"]
}
```

**Behavior**:
- If `mode: "automatic"`: Executes full flow automatically (current behavior)
- If `mode: "manual"`: Creates cart, initializes simulation state, returns immediately. Simulation waits for step commands.

### 2. Manual Control Endpoints

All endpoints under: `/dev/team-carts/{teamCartId}/simulation/`

#### 2.1. Trigger Members to Join

**`POST /dev/team-carts/{teamCartId}/simulation/members-join`**

Request body (optional):
```json
{
  "delayBetweenMembersMs": 1000  // Optional: override default from initialization
}
```

Response:
```json
{
  "status": "MembersJoining",
  "currentStep": "MembersJoining",
  "membersToJoin": ["+84901234561", "+84901234562"],
  "estimatedCompletionTimeUtc": "2025-12-12T10:30:05Z"
}
```

**Behavior**:
- Validates simulation is in `WaitingForMembersJoin` state
- Starts background task that joins members one-by-one
- Each member joins with delay between (`memberJoinDelayMs` or request override)
- Updates state to `MembersJoined` when complete

#### 2.2. Trigger Item Addition Phase

**`POST /dev/team-carts/{teamCartId}/simulation/start-adding-items`**

Request body (optional):
```json
{
  "delayBetweenItemsMs": 1500  // Optional: override default
}
```

Response:
```json
{
  "status": "AddingItems",
  "currentStep": "AddingItems",
  "membersAddingItems": ["+84901234560", "+84901234561", "+84901234562"],
  "estimatedCompletionTimeUtc": "2025-12-12T10:30:20Z"
}
```

**Behavior**:
- Validates simulation is in `MembersJoined` state
- Starts background task that adds items for each member
- Each member adds 1-2 random items
- Delay between each item addition (`itemAdditionDelayMs` or request override)
- Updates state to `ItemsAdded` when complete

#### 2.3. Trigger Mark Ready Phase

**`POST /dev/team-carts/{teamCartId}/simulation/mark-ready`**

Response:
```json
{
  "status": "MembersReady",
  "currentStep": "MembersReady",
  "readyMembers": ["+84901234560", "+84901234561", "+84901234562"]
}
```

**Behavior**:
- Validates simulation is in `ItemsAdded` state
- Marks all members (including host) as ready
- Updates state to `AllMembersReady`

#### 2.4. Trigger Lock Cart

**`POST /dev/team-carts/{teamCartId}/simulation/lock`**

Response:
```json
{
  "status": "Locked",
  "currentStep": "Locked",
  "quoteVersion": 1,
  "grandTotal": 125.50,
  "lockedAtUtc": "2025-12-12T10:30:35Z"
}
```

**Behavior**:
- Validates simulation is in `AllMembersReady` state
- Host locks the cart for payment
- Returns quote version and grand total
- Updates state to `Locked`

#### 2.5. Trigger Payment Phase

**`POST /dev/team-carts/{teamCartId}/simulation/start-payments`**

Request body (optional):
```json
{
  "delayBetweenPaymentsMs": 2000  // Optional: override default
}
```

Response:
```json
{
  "status": "ProcessingPayments",
  "currentStep": "ProcessingPayments",
  "membersToPay": ["+84901234560", "+84901234561", "+84901234562"],
  "quoteVersion": 1,
  "estimatedCompletionTimeUtc": "2025-12-12T10:30:45Z"
}
```

**Behavior**:
- Validates simulation is in `Locked` state
- Starts background task that commits payments one-by-one
- Each member commits COD payment with delay between (`memberPaymentDelayMs` or request override)
- Updates state to `AllPaymentsCommitted` when complete

#### 2.6. Trigger Convert to Order

**`POST /dev/team-carts/{teamCartId}/simulation/convert`**

Request body (optional, for address):
```json
{
  "deliveryAddress": {
    "street": "123 Test Street",
    "city": "Test City",
    "state": "CA",
    "postalCode": "12345",
    "country": "USA"
  },
  "deliveryNotes": "Simulated order"
}
```

Response:
```json
{
  "status": "Completed",
  "currentStep": "Converted",
  "orderId": "guid",
  "convertedAtUtc": "2025-12-12T10:30:50Z"
}
```

**Behavior**:
- Validates simulation is in `AllPaymentsCommitted` state
- Host converts cart to order
- Uses provided address or defaults
- Updates state to `Completed`
- Cleans up simulation state

### Error Responses

All manual control endpoints return standard error responses:

**409 Conflict** - Invalid state transition:
```json
{
  "code": "InvalidSimulationState",
  "message": "Cannot trigger members-join. Current state: 'ItemsAdded'. Expected state: 'WaitingForMembersJoin'."
}
```

**404 Not Found** - Simulation not found:
```json
{
  "code": "SimulationNotFound",
  "message": "No active simulation found for team cart '{teamCartId}'."
}
```

**409 Conflict** - Phase already in progress:
```json
{
  "code": "PhaseInProgress",
  "message": "Members are already joining. Please wait for completion."
}
```

## State Machine

### Simulation States

```
Initialized
  ↓ (POST /simulation/members-join)
WaitingForMembersJoin
  ↓ (background: members join one-by-one)
MembersJoined
  ↓ (POST /simulation/start-adding-items)
AddingItems
  ↓ (background: items added with delays)
ItemsAdded
  ↓ (POST /simulation/mark-ready)
AllMembersReady
  ↓ (POST /simulation/lock)
Locked
  ↓ (POST /simulation/start-payments)
ProcessingPayments
  ↓ (background: payments committed one-by-one)
AllPaymentsCommitted
  ↓ (POST /simulation/convert)
Completed
```

### State Transitions

| Current State | Valid Next Actions | Invalid Actions Return Error |
|---------------|-------------------|------------------------------|
| `Initialized` | `members-join` | All others |
| `WaitingForMembersJoin` | None (phase in progress) | All (409 PhaseInProgress) |
| `MembersJoined` | `start-adding-items` | All others |
| `AddingItems` | None (phase in progress) | All (409 PhaseInProgress) |
| `ItemsAdded` | `mark-ready` | All others |
| `AllMembersReady` | `lock` | All others |
| `Locked` | `start-payments` | All others |
| `ProcessingPayments` | None (phase in progress) | All (409 PhaseInProgress) |
| `AllPaymentsCommitted` | `convert` | All others |
| `Completed` | None (simulation complete) | All (409 SimulationComplete) |

## Implementation Details

### Data Model Changes

**Enhanced Run Record**:
```csharp
private sealed record Run(
    Guid RunId,
    Guid TeamCartId,
    string Scenario,
    string Mode,  // NEW: "automatic" | "manual"
    string CurrentState,  // NEW: current simulation state
    DateTime StartedAtUtc,
    Guid HostUserId,
    List<Guid> MemberUserIds,
    SimulationDelays Delays,  // NEW: store delays for manual control
    CancellationTokenSource? CancellationTokenSource  // NEW: for cancellation
);
```

**Simulation State Enum**:
```csharp
public enum SimulationState
{
    Initialized,
    WaitingForMembersJoin,
    MembersJoining,  // In progress
    MembersJoined,
    AddingItems,  // In progress
    ItemsAdded,
    AllMembersReady,
    Locked,
    ProcessingPayments,  // In progress
    AllPaymentsCommitted,
    Completed,
    Failed
}
```

### Service Interface Changes

**Enhanced ITeamCartFlowSimulator**:
```csharp
public interface ITeamCartFlowSimulator
{
    // Existing methods (unchanged for backward compatibility)
    Task<SimulationStartResult> SimulateFullFlowAsync(...);
    Task<SimulationStartResult> SimulateMemberActionsAsync(...);
    
    // NEW: Manual control methods
    Task<SimulationActionResult> TriggerMembersJoinAsync(
        Guid teamCartId, 
        int? delayBetweenMembersMs = null, 
        CancellationToken ct = default);
    
    Task<SimulationActionResult> TriggerStartAddingItemsAsync(
        Guid teamCartId, 
        int? delayBetweenItemsMs = null, 
        CancellationToken ct = default);
    
    Task<SimulationActionResult> TriggerMarkReadyAsync(
        Guid teamCartId, 
        CancellationToken ct = default);
    
    Task<SimulationActionResult> TriggerLockAsync(
        Guid teamCartId, 
        CancellationToken ct = default);
    
    Task<SimulationActionResult> TriggerStartPaymentsAsync(
        Guid teamCartId, 
        int? delayBetweenPaymentsMs = null, 
        CancellationToken ct = default);
    
    Task<SimulationActionResult> TriggerConvertAsync(
        Guid teamCartId, 
        DeliveryAddress? address = null, 
        string? notes = null, 
        CancellationToken ct = default);
}
```

### Background Task Management

**State-Aware Background Tasks**:
- Each phase that runs automatically (members-join, adding-items, payments) runs in background
- Maintain state to prevent duplicate triggers
- Update state atomically when phase completes
- Handle cancellation for cleanup

**Example: Members Join Background Task**:
```csharp
private async Task ExecuteMembersJoinAsync(
    Run run,
    List<Guid> memberUserIds,
    string shareToken,
    TimeSpan delayBetweenMembers,
    CancellationToken ct)
{
    run.CurrentState = SimulationState.MembersJoining.ToString();
    
    foreach (var memberId in memberUserIds)
    {
        await DelayAsync(delayBetweenMembers, ct);
        await AsUserAsync(memberId, async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            await sender.Send(new JoinTeamCartCommand(...), ct);
        }, ct);
    }
    
    run.CurrentState = SimulationState.MembersJoined.ToString();
}
```

### Delay Configuration

**New Delay Properties** (added to `SimulationDelays`):
```csharp
public sealed class SimulationDelays
{
    // Existing delays (unchanged)
    public int? HostCreateToGuestJoinMs { get; set; }
    public int? GuestJoinToAddItemsMs { get; set; }
    public int? AddItemsToMemberReadyMs { get; set; }
    public int? AllReadyToLockMs { get; set; }
    public int? LockToMemberPaymentMs { get; set; }
    public int? PaymentToConvertMs { get; set; }
    
    // NEW: Manual mode delays
    public int? MemberJoinDelayMs { get; set; }        // Delay between each member joining
    public int? ItemAdditionDelayMs { get; set; }      // Delay between each item addition
    public int? MemberPaymentDelayMs { get; set; }     // Delay between each member payment
}
```

**Default Values** (in `BuildDelays` method):
- `MemberJoinDelayMs`: 1000ms (happyPath), 300ms (fastHappyPath)
- `ItemAdditionDelayMs`: 1500ms (happyPath), 500ms (fastHappyPath)
- `MemberPaymentDelayMs`: 2000ms (happyPath), 500ms (fastHappyPath)

## Usage Examples

### Manual Mode Flow

```bash
# 1. Initialize simulation in manual mode
curl -X POST http://localhost:5000/dev/team-carts/simulate-full-flow \
  -H "Content-Type: application/json" \
  -d '{
    "hostPhone": "+84901234560",
    "memberPhones": ["+84901234561", "+84901234562"],
    "mode": "manual",
    "scenario": "happyPath",
    "delaysMs": {
      "memberJoinDelayMs": 1500,
      "itemAdditionDelayMs": 2000,
      "memberPaymentDelayMs": 2500
    }
  }'

# Response: { "teamCartId": "...", "currentStep": "WaitingForMembersJoin", ... }

# 2. Wait a bit, then trigger members to join
curl -X POST http://localhost:5000/dev/team-carts/{teamCartId}/simulation/members-join

# 3. Check cart state via standard API (no new polling endpoint needed)
curl http://localhost:5000/api/v1/team-carts/{teamCartId}

# 4. When ready, trigger item addition
curl -X POST http://localhost:5000/dev/team-carts/{teamCartId}/simulation/start-adding-items

# 5. Wait and observe items being added...

# 6. Mark all members ready
curl -X POST http://localhost:5000/dev/team-carts/{teamCartId}/simulation/mark-ready

# 7. Lock the cart
curl -X POST http://localhost:5000/dev/team-carts/{teamCartId}/simulation/lock

# 8. Start payment phase
curl -X POST http://localhost:5000/dev/team-carts/{teamCartId}/simulation/start-payments

# 9. Convert to order
curl -X POST http://localhost:5000/dev/team-carts/{teamCartId}/simulation/convert \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryAddress": {
      "street": "123 Test St",
      "city": "Test City",
      "state": "CA",
      "postalCode": "12345",
      "country": "USA"
    }
  }'
```

### Automatic Mode (Backward Compatible)

```bash
# Works exactly as before - no changes needed
curl -X POST http://localhost:5000/dev/team-carts/simulate-full-flow \
  -H "Content-Type: application/json" \
  -d '{
    "hostPhone": "+84901234560",
    "memberPhones": ["+84901234561"],
    "scenario": "happyPath"
  }'
# Or explicitly: "mode": "automatic"
```

## Implementation Plan

### Phase 1: Core Infrastructure
1. Add `mode` parameter to `SimulationRequest` model
2. Enhance `Run` record with state tracking fields
3. Create `SimulationState` enum
4. Update `BuildDelays` to include new delay properties
5. Modify `SimulateFullFlowAsync` to support mode selection

### Phase 2: Manual Control Endpoints
1. Add new endpoints to `DevTeamCarts.cs`
2. Implement manual control methods in `TeamCartFlowSimulator`
3. Add state validation logic
4. Implement background task execution for automatic sub-sequences

### Phase 3: State Management
1. Implement state machine transitions
2. Add state persistence (in-memory via `ConcurrentDictionary`)
3. Handle concurrent access and race conditions
4. Add error handling for invalid state transitions

### Phase 4: Background Tasks
1. Refactor existing background execution into state-aware methods
2. Implement automatic sub-sequences (members-join, adding-items, payments)
3. Add cancellation support
4. Update state atomically after phase completion

### Phase 5: Testing & Documentation
1. Update `TeamCart-Simulator.md` documentation
2. Add usage examples
3. Test backward compatibility (automatic mode)
4. Test manual mode flow end-to-end

## Backward Compatibility

- **Automatic mode is default**: If `mode` is not specified, behavior is unchanged
- **Existing endpoints unchanged**: Current endpoints continue to work
- **No breaking changes**: All existing API contracts preserved
- **Optional parameter**: `mode` parameter is optional, defaults to `"automatic"`

## Testing Considerations

1. **State Transition Testing**: Verify each state transition is valid
2. **Concurrent Access**: Test multiple simulation runs simultaneously
3. **Error Handling**: Test invalid state transitions return proper errors
4. **Background Task Completion**: Verify state updates after background tasks complete
5. **Cancellation**: Test simulation cleanup on cancellation
6. **Backward Compatibility**: Verify automatic mode works identically to current implementation

## Future Enhancements (Out of Scope)

- Individual member control (per-member endpoints)
- Status polling endpoint with real-time updates
- Simulation pause/resume capability
- Simulation replay/step-back functionality
- WebSocket/SignalR integration for real-time status updates

