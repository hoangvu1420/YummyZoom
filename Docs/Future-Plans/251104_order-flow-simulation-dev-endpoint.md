# Dev Order Flow Simulation Endpoint — Detailed Design & Plan

Status: Proposed (Dev/Test only)

Owner: Web/API

Date: 2025-11-04

## Goals

- Allow customer client app to experience realistic order tracking flows (status transitions, timestamps, SignalR, ETags) without a restaurant management app.
- Use existing production commands and domain rules (no fakes of status or events), but expose a dev-only orchestrator to drive the flow.

## Non-Goals

- No production exposure. No relaxation of authorization policies in non-dev environments.
- No new domain transitions; reuse existing Accept/Preparing/Ready/Delivered flows.

## Confirmed Decisions

- Single entrypoint: `POST /api/v1/dev/orders/{orderId}/simulate-flow`.
- Request body selects a predefined scenario and optional timing overrides.
- No auth required for this endpoint. Safe because dev-only and guarded.
- Always start at status = `Placed` for both COD and Online payments.
  - Rationale: Customer enters tracking after successful Stripe confirmation (client) → webhook → order moves to `Placed`.
  - If the order is not yet `Placed` (e.g., `AwaitingPayment`), the simulator returns a 409 (see Behavior).
- Handle authorization for underlying commands by impersonating a restaurant staff principal in-process (scoped), not by bypassing policies.

## Scenarios

1) `happyPath` (default)
   - `Placed` → `Accepted` → `Preparing` → `ReadyForDelivery` → `Delivered`
   - Default delays: 5s, 30s, 45s, 30s

2) `fastHappyPath`
   - Same steps, 1s delay each (demo mode)

3) `rejected`
   - `Placed` → `Rejected`
   - Default delay: 5s

4) `cancelledByRestaurant`
   - `Placed` → `Accepted` → `Cancelled`
   - Default delays: 5s, 10s

Notes:
- All transitions are executed by sending the real Application commands via `ISender`:
  - `AcceptOrderCommand`
  - `MarkOrderPreparingCommand`
  - `MarkOrderReadyForDeliveryCommand`
  - `MarkOrderDeliveredCommand`
  - `RejectOrderCommand`
  - `CancelOrderCommand`
  
Domain events and SignalR broadcasts are triggered naturally.

## Endpoint Spec

`POST /api/v1/dev/orders/{orderId}/simulate-flow`

- Auth: none (dev-only; disabled outside Dev/Test)
- Content-Type: `application/json`

Request Body:
```json
{
  "scenario": "happyPath",           // one of: happyPath, fastHappyPath, rejected, cancelledByRestaurant
  "delaysMs": {                       // optional, overrides per step for fine control
    "placedToAcceptedMs": 5000,
    "acceptedToPreparingMs": 30000,
    "preparingToReadyMs": 45000,
    "readyToDeliveredMs": 30000,
    "placedToRejectedMs": 5000,
    "acceptedToCancelledMs": 10000
  },
  "estimatedDeliveryMinutes": 40      // optional; used for Accept step if applicable
}
```

Response:
- `202 Accepted`
```json
{
  "runId": "b9e2f3a1-...",
  "orderId": "...",
  "scenario": "happyPath",
  "status": "Started",
  "startedAtUtc": "2025-11-04T12:34:56Z",
  "nextStep": "Accept",
  "notes": "Delays applied: 5s, 30s, 45s, 30s"
}
```

Errors:
- `404 Not Found` — feature disabled or order not found
- `409 Conflict` — order not eligible (e.g., status < Placed)
- `409 Conflict` — already running for this order (idempotency)

## Behavior

1) Feature Gate (required):
   - Environment must be Development or Test.
   - `Features.OrderFlowSimulation` must be true.
   - If either check fails → 404 (to avoid advertising the endpoint).

2) Pre-Validation:
   - Load order (Dapper or EF; repository acceptable). Capture `RestaurantId`, `Status`.
   - If `Status` is `AwaitingPayment` → `409 Conflict` (message: "OrderNotPlaced"). Rationale: we only simulate from `Placed` forward.
   - If `Delivered`/`Cancelled`/`Rejected` → `409 Conflict` (terminal state).

3) Orchestration:
   - Scenarios map to ordered command invocations separated by delays.
   - Execution runs in background (`Task.Run`) with a scoped DI container.
   - Maintain an in-memory tracker keyed by `orderId` with `runId`, `scenario`, `startedAtUtc`, `currentStep`, `cancelRequested`.
   - If a run already exists for the order and still active → return `409 Conflict` with that `runId`.

4) Authorization Handling for Commands:
   - Wrap each command invocation in an impersonation scope providing a principal with:
     - Role/permission claim: `permission = RestaurantStaff:{restaurantId}` (or `RestaurantOwner:{restaurantId}`)
   - Implementation approach:
     - Add `IDevImpersonationService` that can create a scoped `IUser` implementation returning the forged principal for the duration of the send.
     - The Application authorization behaviors will see this principal and allow the command.
   - No policy bypass, no code changes to command handlers.

5) Steps & Commands

Happy Path (default/fast):
```
delay(placedToAccepted)
→ AcceptOrderCommand(orderId, restaurantId, estimatedDeliveryTime)
delay(acceptedToPreparing)
→ MarkOrderPreparingCommand(orderId, restaurantId)
delay(preparingToReady)
→ MarkOrderReadyForDeliveryCommand(orderId, restaurantId)
delay(readyToDelivered)
→ MarkOrderDeliveredCommand(orderId, restaurantId, deliveredAtUtc = now)
```

Rejected:
```
delay(placedToRejected)
→ RejectOrderCommand(orderId, restaurantId, reason = "Simulated")
```

Cancelled By Restaurant:
```
delay(placedToAccepted)
→ AcceptOrderCommand(...)
delay(acceptedToCancelled)
→ CancelOrderCommand(orderId, restaurantId, actingUserId = null, reason = "Simulated")
```

6) SignalR & Caching
   - Each command triggers domain events → SignalR broadcasts (customer + restaurant groups).
   - `LastUpdateTimestamp` and ETags update accordingly; polling endpoints (`/orders/{id}` and `/orders/{id}/status`) reflect changes.

## Data Model / Storage

- No persistent storage. In-memory tracker suffices for dev.
- `ConcurrentDictionary<Guid, SimulationRun>` with fields: runId, orderId, scenario, startedAt, stepIndex, cancellationFlag.

## Observability / Diagnostics

- Log each step: orderId, scenario, transition, planned delay, actual timestamps.
- On errors, stop simulation and log cause.
- Optionally expose a `GET /api/v1/dev/orders/{orderId}/simulation` to read current state (v2).

## Failure Handling

- If a command returns failure (e.g., status invalid due to external interference), stop and log.
- Do not retry by default; surface in logs.

## Security

- Endpoint only enabled in Development/Test & when `Features.OrderFlowSimulation=true`.
- No authentication on the endpoint itself (as requested), but impersonation only happens inside the orchestrator scope for the specific restaurant and for command execution only.
- Ensure this feature flag is false in all production configs.

## Implementation Plan

1) Feature Flag
   - Update `src/Web/Configuration/FeatureFlagsOptions.cs` to add:
     ```csharp
     public bool OrderFlowSimulation { get; set; } = false;
     ```
   - Wire in `src/Web/DependencyInjection.cs` (already binds `Features` section).
   - Set `Features:OrderFlowSimulation = true` in `appsettings.Development.json`.

2) Dev Endpoint
   - New file: `src/Web/Endpoints/DevOrders.cs`
   - Map:
     ```csharp
     group.MapPost("/dev/orders/{orderId:guid}/simulate-flow", ...);
     ```
   - Guard: environment + feature flag; if disabled, return 404.
   - Read and validate request, check order status via lightweight query (Dapper) or repository.
   - Start background run via simulator service and return 202 with `runId`.

3) Simulator Service
   - New files under `src/Web/Services/OrderFlowSimulator/`:
     - `IOrderFlowSimulator.cs`
     - `OrderFlowSimulator.cs`
   - Register as singleton. Maintain in-memory runs. Use `IServiceScopeFactory` to create scopes per step.
   - Use `ISender` to send commands.
   - Delay by `Task.Delay` between steps.

4) Impersonation Helper
   - New file `src/Web/Security/DevImpersonationService.cs` implementing `IDevImpersonationService`.
   - Provides `RunAsRestaurantStaffAsync(Guid restaurantId, Func<Task> action)`:
     - Creates a scope replacing `IUser` with an implementation that returns a principal containing `permission = RestaurantStaff:{restaurantId}` (and a synthetic user id).
     - Executes action; disposes scope.

5) Docs
   - This plan document.
   - Optional: add a short internal doc under `Docs/Development-Guidelines/` to explain how to use the endpoint during demos.

6) (Optional) Tests
   - Minimal Web.ApiContractTests for the dev endpoint (behind Test environment factory) to assert 202 and basic schema.

## Open Questions / Notes

- Should we allow starting from statuses beyond `Placed` (e.g., re-run from `Accepted`)? For v1, we keep simple: require >= `Placed`; the simulator will skip steps already passed.
- Time travel for timestamps? For now, use `DateTime.UtcNow` per step; no backdating.
- Payment webhooks: We purposely do not simulate payment transitions; confirmed we start at `Placed`.

