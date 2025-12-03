# TeamCart Flow Simulator

Dev/Test utility for simulating complete TeamCart workflows with real users and automated actions.

## Overview

The TeamCart simulator automates the collaborative ordering flow for development and testing purposes. It uses phone numbers to identify existing users and simulates their actions through the entire TeamCart lifecycle.

## Feature Flag

Enable in `appsettings.Development.json`:

```json
{
  "Features": {
    "TeamCartFlowSimulation": true
  }
}
```

## Endpoints

### 1. Full Flow Simulation

**`POST /dev/team-carts/simulate-full-flow`**

Simulates the complete TeamCart journey from creation to order conversion.

**Request Body:**
```json
{
  "hostPhone": "+84901234560",
  "memberPhones": [
    "+84901234561"
  ],
  "scenario": "happyPath",
  "delaysMs": {
    "hostCreateToGuestJoinMs": 2000,
    "guestJoinToAddItemsMs": 20000,
    "addItemsToMemberReadyMs": 3000,
    "allReadyToLockMs": 15000,
    "lockToMemberPaymentMs": 5000,
    "paymentToConvertMs": 1500
  }
}
```

**Flow Steps:**
1. Host creates TeamCart
2. Members join using share token
3. All members (including host) add 1-2 random menu items
4. All members mark themselves ready
5. Host locks cart for payment
6. All members commit COD payments
7. Host converts cart to order

**Response:** `202 Accepted`
```json
{
  "runId": "guid",
  "teamCartId": "guid",
  "scenario": "happyPath",
  "status": "Started",
  "startedAtUtc": "2025-12-03T10:30:00Z",
  "nextStep": "MembersJoining",
  "simulatedMembers": ["+1234567891", "+1234567892"]
}
```

### 2. Member-Only Simulation

**`POST /dev/team-carts/{teamCartId}/simulate-members`**

Simulates only member actions on an existing TeamCart. Host must manually perform their actions (lock and convert).

**Request Body:**
```json
{
  "memberPhones": ["+1234567891", "+1234567892"],
  "scenario": "memberCollaboration",
  "delaysMs": {
    "hostCreateToGuestJoinMs": 1000,
    "guestJoinToAddItemsMs": 1000,
    "addItemsToMemberReadyMs": 2000,
    "lockToMemberPaymentMs": 2000
  }
}
```

**Flow Steps:**
1. Members join using share token
2. Members add 1-2 random menu items
3. Members mark themselves ready
4. Members commit COD payments (after host locks)

**Prerequisites:**
- TeamCart must exist and be in `Open` status
- Host must be a member of the cart

## Scenarios

### `happyPath` (Default)
Standard timing with realistic delays (2-3 seconds between steps).

### `fastHappyPath`
Compressed timing for quick testing (300-500ms between steps).

### `memberCollaboration`
Member-only flow with moderate delays (1-2 seconds between steps).

## Configuration

### Delays

All delays are optional. Defaults vary by scenario:

| Delay | happyPath | fastHappyPath |
|-------|-----------|---------------|
| `hostCreateToGuestJoinMs` | 2000ms | 500ms |
| `guestJoinToAddItemsMs` | 2000ms | 500ms |
| `addItemsToMemberReadyMs` | 3000ms | 500ms |
| `allReadyToLockMs` | 1500ms | 300ms |
| `lockToMemberPaymentMs` | 3000ms | 500ms |
| `paymentToConvertMs` | 1500ms | 300ms |

### Custom Delays Example

```json
{
  "scenario": "happyPath",
  "delaysMs": {
    "guestJoinToAddItemsMs": 5000,
    "addItemsToMemberReadyMs": 10000
  }
}
```

Unspecified delays will use scenario defaults.

## User Setup

### Prerequisites

All users must exist in the database with phone numbers:

```sql
-- Check existing users
SELECT "Id", "UserName", "PhoneNumber" 
FROM "Users" 
WHERE "PhoneNumber" IN ('+1234567890', '+1234567891', '+1234567892');
```

### Creating Test Users

Use the registration endpoint or insert directly:

```json
POST /api/v1/users/register
{
  "name": "Test User 1",
  "email": "testuser1@example.com",
  "phoneNumber": "+1234567890",
  "password": "Password123!"
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

## Error Handling

### Common Errors

**`UserNotFound`**
```json
{
  "code": "SimulationError",
  "message": "Host user with phone '+1234567890' not found."
}
```

**`TeamCartNotFound`** (member-only)
```json
{
  "code": "SimulationError",
  "message": "TeamCart 'guid' not found."
}
```

**`InvalidState`** (member-only)
```json
{
  "code": "SimulationError",
  "message": "TeamCart must be in 'Open' state. Current state: 'Locked'"
}
```

**`AlreadyRunning`**
```json
{
  "code": "SimulationError",
  "message": "A simulation is already running for this team cart."
}
```

## Usage Examples

### Quick Happy Path Test

```bash
curl -X POST http://localhost:5000/dev/team-carts/simulate-full-flow \
  -H "Content-Type: application/json" \
  -d '{
    "hostPhone": "+1234567890",
    "memberPhones": ["+1234567891", "+1234567892"],
    "scenario": "fastHappyPath"
  }'
```

### Test Member Collaboration

```bash
# 1. Manually create TeamCart via UI or API
# 2. Note the teamCartId and shareToken

# 3. Simulate members joining and collaborating
curl -X POST http://localhost:5000/dev/team-carts/{teamCartId}/simulate-members \
  -H "Content-Type: application/json" \
  -d '{
    "memberPhones": ["+1234567891", "+1234567892"],
    "scenario": "memberCollaboration"
  }'

# 4. Manually lock cart and convert to order as host
```

## Background Execution

Simulations run **asynchronously** in the background:
- Endpoint returns `202 Accepted` immediately
- Simulation executes in a background task
- Track progress by querying TeamCart state via standard APIs

## Limitations

- Only one simulation per TeamCart at a time
- Dev/Test environments only (feature-gated)
- COD payments only
- Random menu item selection (not configurable)
- No customization selections on items
- No coupon/tip application

## Implementation Details

**Files:**
- `src/Web/Services/TeamCartFlowSimulator/TeamCartFlowSimulator.cs`
- `src/Web/Services/TeamCartFlowSimulator/ITeamCartFlowSimulator.cs`
- `src/Web/Services/TeamCartFlowSimulator/Models/TeamCartSimulationModels.cs`
- `src/Web/Endpoints/DevTeamCarts.cs`
- `src/Web/Security/DevImpersonationService.cs` (user impersonation)

**Service Registration:** Singleton in `DependencyInjection.cs`

**Impersonation:** Uses `IDevImpersonationService.RunAsUserAsync()` to execute commands as specific users without authentication.
