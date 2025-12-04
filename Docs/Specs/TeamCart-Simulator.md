# TeamCart Flow Simulator

Dev/Test utility for simulating complete TeamCart workflows with real users and automated actions.

## Overview

The TeamCart simulator automates the collaborative ordering flow for development and testing purposes. It uses phone numbers to identify existing users and simulates their actions through the entire TeamCart lifecycle.

**Two Modes Available:**
- **Automatic Mode**: Runs the full flow automatically (default)
- **Manual Mode**: Step-by-step control for fine-grained testing

## Feature Flag

Enable in `appsettings.Development.json`:

```json
{
  "Features": {
    "TeamCartFlowSimulation": true
  }
}
```

## User Setup

All users must exist in the database with phone numbers:

```sql
-- Check existing users
SELECT "Id", "UserName", "PhoneNumber" 
FROM "AspNetUsers" 
WHERE "PhoneNumber" IN ('+84901234560', '+84901234561', '+84901234562');
```

### Creating Test Users

Use the registration endpoint or insert directly:

```json
POST /api/v1/users/register
{
  "name": "Test User 1",
  "email": "testuser1@example.com",
  "phoneNumber": "+84901234560",
  "password": "Password123!"
}
```

## Automatic Mode (Full Flow)

Simulates the complete TeamCart journey from creation to order conversion automatically.

### Endpoint

**`POST /dev/team-carts/simulate-full-flow`**

### Request Body

```json
{
  "hostPhone": "+84901234560",
  "memberPhones": [
    "+84901234561",
    "+84901234562"
  ],
  "mode": "automatic",
  "scenario": "happyPath",
  "restaurantId": "optional-guid",
  "delaysMs": {
    "hostCreateToGuestJoinMs": 2000,
    "guestJoinToAddItemsMs": 2000,
    "addItemsToMemberReadyMs": 3000,
    "allReadyToLockMs": 1500,
    "lockToMemberPaymentMs": 3000,
    "paymentToConvertMs": 1500
  }
}
```

### Response: `202 Accepted`

```json
{
  "runId": "guid",
  "teamCartId": "guid",
  "shareToken": "token",
  "scenario": "happyPath",
  "mode": "Automatic",
  "status": "Started",
  "startedAtUtc": "2025-12-12T10:30:00Z",
  "nextStep": "MembersJoining",
  "currentStep": "Initialized",
  "simulatedMembers": ["+84901234561", "+84901234562"]
}
```

### Automatic Flow Steps

1. Host creates TeamCart
2. Members join using share token (one-by-one with delays)
3. All members (including host) add 1-2 random menu items (with delays between items)
4. All members mark themselves ready
5. Host locks cart for payment
6. All members commit COD payments (one-by-one with delays)
7. Host converts cart to order

### Usage Example

```bash
curl -X POST http://localhost:5000/dev/team-carts/simulate-full-flow \
  -H "Content-Type: application/json" \
  -d '{
    "hostPhone": "+84901234560",
    "memberPhones": ["+84901234561", "+84901234562"],
    "mode": "automatic",
    "scenario": "happyPath"
  }'
```

## Manual Mode (Step-by-Step Control)

Provides fine-grained control over each phase of the simulation. Each step must be triggered manually.

### 1. Initialize Simulation

**`POST /dev/team-carts/simulate-full-flow`** (with `mode: "manual"`)

Request:
```json
{
  "hostPhone": "+84901234560",
  "memberPhones": ["+84901234561", "+84901234562"],
  "mode": "manual",
  "scenario": "happyPath",
  "delaysMs": {
    "memberJoinDelayMs": 1000,
    "itemAdditionDelayMs": 1500,
    "memberPaymentDelayMs": 2000
  }
}
```

Response:
```json
{
  "runId": "guid",
  "teamCartId": "guid",
  "shareToken": "token",
  "scenario": "happyPath",
  "mode": "Manual",
  "status": "Initialized",
  "startedAtUtc": "2025-12-12T10:30:00Z",
  "nextStep": "WaitingForMembersJoin",
  "currentStep": "WaitingForMembersJoin",
  "simulatedMembers": ["+84901234561", "+84901234562"]
}
```

### 2. Trigger Members to Join

**`POST /dev/team-carts/{teamCartId}/simulation/members-join`**

Request (optional):
```json
{
  "delayBetweenMembersMs": 1500
}
```

Response:
```json
{
  "teamCartId": "guid",
  "status": "MembersJoining",
  "currentStep": "MembersJoining",
  "actionPerformedAtUtc": "2025-12-12T10:30:05Z",
  "members": ["guid1", "guid2"],
  "estimatedCompletionTimeUtc": "2025-12-12T10:30:08Z"
}
```

**Behavior:**
- Validates simulation is in `WaitingForMembersJoin` state
- Starts background task that joins members one-by-one
- Each member joins with delay between (configurable)
- Updates state to `MembersJoined` when complete

### 3. Trigger Item Addition Phase

**`POST /dev/team-carts/{teamCartId}/simulation/start-adding-items`**

Request (optional):
```json
{
  "delayBetweenItemsMs": 2000
}
```

Response:
```json
{
  "teamCartId": "guid",
  "status": "AddingItems",
  "currentStep": "AddingItems",
  "actionPerformedAtUtc": "2025-12-12T10:30:10Z",
  "members": ["guid1", "guid2", "guid3"]
}
```

**Behavior:**
- Validates simulation is in `MembersJoined` state
- Starts background task that adds items for each member
- Each member adds 1-2 random items
- Delay between each item addition (configurable)
- Updates state to `ItemsAdded` when complete

### 4. Mark All Members Ready

**`POST /dev/team-carts/{teamCartId}/simulation/mark-ready`**

Response:
```json
{
  "teamCartId": "guid",
  "status": "MembersReady",
  "currentStep": "AllMembersReady",
  "actionPerformedAtUtc": "2025-12-12T10:30:25Z",
  "members": ["guid1", "guid2", "guid3"]
}
```

**Behavior:**
- Validates simulation is in `ItemsAdded` state
- Marks all members (including host) as ready
- Updates state to `AllMembersReady`

### 5. Lock Cart for Payment

**`POST /dev/team-carts/{teamCartId}/simulation/lock`**

Response:
```json
{
  "teamCartId": "guid",
  "status": "Locked",
  "currentStep": "Locked",
  "actionPerformedAtUtc": "2025-12-12T10:30:30Z",
  "quoteVersion": 1
}
```

**Behavior:**
- Validates simulation is in `AllMembersReady` state
- Host locks the cart for payment
- Returns quote version
- Updates state to `Locked`

### 6. Trigger Payment Phase

**`POST /dev/team-carts/{teamCartId}/simulation/start-payments`**

Request (optional):
```json
{
  "delayBetweenPaymentsMs": 2000
}
```

Response:
```json
{
  "teamCartId": "guid",
  "status": "ProcessingPayments",
  "currentStep": "ProcessingPayments",
  "actionPerformedAtUtc": "2025-12-12T10:30:35Z",
  "members": ["guid1", "guid2", "guid3"],
  "quoteVersion": 1,
  "estimatedCompletionTimeUtc": "2025-12-12T10:30:41Z"
}
```

**Behavior:**
- Validates simulation is in `Locked` state
- Starts background task that commits payments one-by-one
- Each member commits COD payment with delay between (configurable)
- Updates state to `AllPaymentsCommitted` when complete

### 7. Convert to Order

**`POST /dev/team-carts/{teamCartId}/simulation/convert`**

Request (optional):
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
  "teamCartId": "guid",
  "status": "Completed",
  "currentStep": "Completed",
  "actionPerformedAtUtc": "2025-12-12T10:30:45Z",
  "orderId": "guid"
}
```

**Behavior:**
- Validates simulation is in `AllPaymentsCommitted` state
- Host converts cart to order
- Uses provided address or defaults
- Updates state to `Completed`
- Cleans up simulation state

## Complete Manual Mode Example

```bash
# Step 1: Initialize simulation in manual mode
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

# Response contains teamCartId - save it for next steps
# Example: teamCartId = "abc123-def456-..."

# Step 2: Wait a bit, then trigger members to join
curl -X POST http://localhost:5000/dev/team-carts/abc123-def456-.../simulation/members-join \
  -H "Content-Type: application/json"

# Step 3: Check cart state via standard API (optional)
curl http://localhost:5000/api/v1/team-carts/abc123-def456-...

# Step 4: When ready, trigger item addition
curl -X POST http://localhost:5000/dev/team-carts/abc123-def456-.../simulation/start-adding-items \
  -H "Content-Type: application/json"

# Step 5: Wait and observe items being added...

# Step 6: Mark all members ready
curl -X POST http://localhost:5000/dev/team-carts/abc123-def456-.../simulation/mark-ready \
  -H "Content-Type: application/json"

# Step 7: Lock the cart
curl -X POST http://localhost:5000/dev/team-carts/abc123-def456-.../simulation/lock \
  -H "Content-Type: application/json"

# Step 8: Start payment phase
curl -X POST http://localhost:5000/dev/team-carts/abc123-def456-.../simulation/start-payments \
  -H "Content-Type: application/json"

# Step 9: Convert to order
curl -X POST http://localhost:5000/dev/team-carts/abc123-def456-.../simulation/convert \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryAddress": {
      "street": "123 Test St",
      "city": "Test City",
      "state": "CA",
      "postalCode": "12345",
      "country": "USA"
    },
    "deliveryNotes": "Simulated order"
  }'
```

## Scenarios

### `happyPath` (Default)
Standard timing with realistic delays (2-3 seconds between steps).

### `fastHappyPath`
Compressed timing for quick testing (300-500ms between steps).

### `memberCollaboration`
Member-only flow with moderate delays (1-2 seconds between steps).

## Delay Configuration

All delays are optional and specified in milliseconds. Defaults vary by scenario.

### Automatic Mode Delays

| Delay Property | happyPath | fastHappyPath |
|----------------|-----------|---------------|
| `hostCreateToGuestJoinMs` | 2000ms | 500ms |
| `guestJoinToAddItemsMs` | 2000ms | 500ms |
| `addItemsToMemberReadyMs` | 3000ms | 500ms |
| `allReadyToLockMs` | 1500ms | 300ms |
| `lockToMemberPaymentMs` | 3000ms | 500ms |
| `paymentToConvertMs` | 1500ms | 300ms |

### Manual Mode Delays

| Delay Property | happyPath | fastHappyPath |
|----------------|-----------|---------------|
| `memberJoinDelayMs` | 1000ms | 300ms |
| `itemAdditionDelayMs` | 1500ms | 500ms |
| `memberPaymentDelayMs` | 2000ms | 500ms |

### Custom Delays Example

```json
{
  "scenario": "happyPath",
  "delaysMs": {
    "memberJoinDelayMs": 5000,
    "itemAdditionDelayMs": 3000,
    "memberPaymentDelayMs": 4000
  }
}
```

Unspecified delays will use scenario defaults.

## State Machine

### Simulation States

- `Initialized` - Simulation created, waiting for commands
- `WaitingForMembersJoin` - Ready to trigger members join
- `MembersJoining` - Members are joining (in progress)
- `MembersJoined` - All members have joined
- `AddingItems` - Items are being added (in progress)
- `ItemsAdded` - All items have been added
- `AllMembersReady` - All members marked ready
- `Locked` - Cart locked for payment
- `ProcessingPayments` - Payments being processed (in progress)
- `AllPaymentsCommitted` - All payments completed
- `Completed` - Order converted, simulation complete
- `Failed` - Simulation failed

### Valid State Transitions

| Current State | Valid Next Action |
|---------------|-------------------|
| `Initialized` / `WaitingForMembersJoin` | `members-join` |
| `MembersJoined` | `start-adding-items` |
| `ItemsAdded` | `mark-ready` |
| `AllMembersReady` | `lock` |
| `Locked` | `start-payments` |
| `AllPaymentsCommitted` | `convert` |
| `Completed` | None (simulation complete) |

**Note:** States marked as "in progress" (`MembersJoining`, `AddingItems`, `ProcessingPayments`) cannot accept new commands until they complete.

## Error Handling

### Common Errors

**`SimulationNotFound` (404)**
```json
{
  "code": "SimulationNotFound",
  "message": "No active simulation found for team cart '{teamCartId}'."
}
```

**`InvalidSimulationState` (409)**
```json
{
  "code": "InvalidSimulationState",
  "message": "Cannot trigger members-join. Current state: 'ItemsAdded'. Expected state: 'WaitingForMembersJoin'."
}
```

**`PhaseInProgress` (409)**
```json
{
  "code": "PhaseInProgress",
  "message": "Cannot trigger start-adding-items. Phase is already in progress. Current state: 'AddingItems'."
}
```

**`SimulationComplete` (409)**
```json
{
  "code": "SimulationComplete",
  "message": "Cannot trigger members-join. Simulation is already completed."
}
```

**`UserNotFound`**
```json
{
  "code": "SimulationError",
  "message": "Host user with phone '+84901234560' not found."
}
```

**`TeamCartNotFound`**
```json
{
  "code": "SimulationError",
  "message": "TeamCart 'guid' not found."
}
```

**`AlreadyRunning`**
```json
{
  "code": "SimulationError",
  "message": "A simulation is already running for this team cart."
}
```

## Menu Items

The simulator automatically:
- Queries available menu items from the TeamCart's restaurant
- Selects 1-2 random items per member
- Assigns random quantities (1-2) per item

**Requirement:** Restaurant must have at least one available menu item.

## Payment Flow

All payments are **Cash on Delivery (COD)** for simplicity. No online payment gateway integration required.

## Background Execution

- Automatic mode: Runs entirely in background
- Manual mode: Steps that execute automatically (members-join, adding-items, payments) run in background after being triggered
- Endpoints return immediately while background tasks execute
- Track progress by querying TeamCart state via standard APIs

## Limitations

- Only one simulation per TeamCart at a time
- Dev/Test environments only (feature-gated)
- COD payments only
- Random menu item selection (not configurable)
- No customization selections on items
- No coupon/tip application in simulation

## Implementation Details

**Files:**
- `src/Web/Services/TeamCartFlowSimulator/TeamCartFlowSimulator.cs`
- `src/Web/Services/TeamCartFlowSimulator/ITeamCartFlowSimulator.cs`
- `src/Web/Services/TeamCartFlowSimulator/Models/TeamCartSimulationModels.cs`
- `src/Web/Endpoints/DevTeamCarts.cs`
- `src/Web/Security/DevImpersonationService.cs` (user impersonation)

**Service Registration:** Singleton in `DependencyInjection.cs`

**Impersonation:** Uses `IDevImpersonationService.RunAsUserAsync()` to execute commands as specific users without authentication.

## Quick Start

### Automatic Mode (Recommended for Quick Testing)

```bash
curl -X POST http://localhost:5000/dev/team-carts/simulate-full-flow \
  -H "Content-Type: application/json" \
  -d '{
    "hostPhone": "+84901234560",
    "memberPhones": ["+84901234561"],
    "scenario": "fastHappyPath"
  }'
```

### Manual Mode (Recommended for Detailed Testing)

```bash
# 1. Initialize
curl -X POST http://localhost:5000/dev/team-carts/simulate-full-flow \
  -H "Content-Type: application/json" \
  -d '{
    "hostPhone": "+84901234560",
    "memberPhones": ["+84901234561"],
    "mode": "manual"
  }'

# 2. Execute steps one by one using the teamCartId from response
# See "Complete Manual Mode Example" section above
```

