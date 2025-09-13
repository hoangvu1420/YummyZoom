# TeamCart — Idempotency for Client‑Triggered Payment Commands

This document defines the implementation plan to add end‑to‑end idempotency for sensitive TeamCart actions initiated by clients. It aligns with the current architecture (Minimal APIs + MediatR + Redis RT store + Stripe) and builds on existing reliability patterns.

Goals:
- Prevent duplicate external effects (e.g., duplicate Stripe PaymentIntents, double conversion to Order) from client retries, double‑clicks, or network glitches.
- Provide deterministic, repeatable responses to the same client request when accompanied by an idempotency key.
- Keep handlers safe to re‑execute and make endpoint behavior predictable under retries.

Non‑Goals:
- Global idempotency across all endpoints. This plan targets TeamCart actions that mutate state and/or call external systems.
- Strong cross‑service atomicity. We will design safe, consistent behavior but not attempt distributed transactions.

## Scope

Endpoints to support `Idempotency-Key` header:
- `POST /api/v1/team-carts/{id}/payments/online`
- `POST /api/v1/team-carts/{id}/lock`
- `POST /api/v1/team-carts/{id}/convert`

Key files (existing):
- `src/Web/Endpoints/TeamCarts.cs`
- `src/Application/TeamCarts/Commands/InitiateMemberOnlinePayment/*`
- `src/Application/TeamCarts/Commands/ConvertTeamCartToOrder/*`
- `src/Application/TeamCarts/Commands/HandleTeamCartStripeWebhook/*`
- `src/Infrastructure/StateStores/TeamCartStore/RedisTeamCartStore.cs`

New components to add:
- `IIdempotencyStore` (Application or Infrastructure Abstractions)
- `RedisIdempotencyStore` (Infrastructure)
- `IdempotencyBehavior<TRequest,TResponse>` (Application/MediatR Behavior)
- Optional: `IdempotencyEndpointFilter` for Minimal APIs (Web) if we prefer endpoint‑layer wrapping

## High‑Level Design

We support a client‑supplied `Idempotency-Key` header. For requests that include it, we:
1) Compute a deterministic fingerprint of the request context: action + route values + userId + command payload (canonical JSON) + optional version tag.
2) Attempt to start an idempotency record in Redis using `SET NX` with a short pending TTL (e.g., 60s) to guard against concurrent duplicates.
3) If a completed record exists, return the stored outcome (status + body) immediately.
4) If a pending record exists (in‑flight), return `202 Accepted` or `409 Conflict` with `Retry-After`.
5) Execute the handler; on success or handled failure, persist the outcome (status, body, content type) with a longer TTL (24–48h), replacing the pending entry.
6) For Stripe PaymentIntent creation, pass the same idempotency key to Stripe’s client request options to gain Stripe‑side dedupe as well.

Handlers must remain idempotent at the domain level: re‑executing a command should either be a no‑op or return the prior result when state already transitioned.

## API Contract

- Header: `Idempotency-Key: <opaque ASCII, 8–128 chars>`
- Success replay: Duplicate requests (same key + same fingerprint) return the original status and response body.
- In‑flight duplicate: If the same key is being processed, respond with `202 Accepted` (recommended) or `409 Conflict` and include `Retry-After: 2`.
- Key reuse with different fingerprint: `409 Conflict` with problem code `Idempotency.MismatchedFingerprint`.
- Recommended: Require the header for `payments/online` and `convert`; optional for `lock` (but supported).

## Storage Model (Redis)

Key pattern:
- `idemp:{scope}:{key}`
- Scope format: `teamcart:{cartId}:{userId}:{action}`

Value (JSON):
```
{
  "state": "pending" | "completed",
  "fingerprint": "<sha256_base64>",
  "statusCode": 200,
  "contentType": "application/json",
  "body": "<serialized response>",
  "completedAt": "2024-01-01T00:00:00Z"
}
```

Semantics:
- Start: `SET NX PX=60000` with `{ state: "pending", fingerprint }`.
- Complete: `SET PX=172800000` with `{ state: "completed", fingerprint, statusCode, contentType, body, completedAt }`.
- Get: `GET` and parse; if completed and fingerprint matches → replay; if pending and fingerprint matches → 202/409.

Security:
- Include `userId` and `cartId` in scope to prevent cross‑user replay.
- TTLs keep sensitive response payloads (e.g., Stripe client_secret) ephemeral. Avoid storing secrets longer than necessary (consider shorter TTL for payment initiation entries, e.g., 2h).

## Components

1) Interface: `IIdempotencyStore`
- Methods:
  - `Task<IdempotencyRecord?> TryGetAsync(string scope, string key, CancellationToken ct)`
  - `Task<TryStartResult> TryStartAsync(string scope, string key, string fingerprint, TimeSpan pendingTtl, CancellationToken ct)`
  - `Task CompleteAsync(string scope, string key, IdempotencyCompletedOutcome outcome, TimeSpan completedTtl, CancellationToken ct)`

2) Redis implementation: `RedisIdempotencyStore`
- Use configured ConnectionMultiplexer for Redis.
- JSON serialize with the project’s `DomainJson` options for consistency.
- Emit structured logs on hits, misses, conflicts, and completes.

3) MediatR behavior: `IdempotencyBehavior<TRequest,TResponse>`
- Enabled for specific commands via marker interface `IIdempotentCommand` or attribute `[Idempotent("action-name")]`.
- Uses `IHttpContextAccessor` to read `Idempotency-Key` and user claims; computes scope and fingerprint.
- Flow:
  - If header missing: pass through to next.
  - `TryGet` → if completed & fingerprint matches: deserialize `TResponse` and short‑circuit.
  - `TryStart` → if false:
    - If existing is completed & fingerprint matches: short‑circuit as above.
    - If existing is pending & fingerprint matches: throw/return Problem 202/409 with `Retry-After`.
    - If fingerprint mismatch: return Problem 409 `Idempotency.MismatchedFingerprint`.
  - Invoke next; on completion, capture response and call `CompleteAsync` with serialized body + status code.

Notes:
- For Minimal APIs returning `IResult`, behavior can store a standardized envelope `{ statusCode, body }`. If `TResponse` is a `Result<T>` wrapper, serialize the DTO.
- Alternatively, implement an `IdempotencyEndpointFilter` at Web layer to wrap `IResult` directly. Use whichever integrates cleaner with current endpoint patterns.

## Fingerprint Strategy

Composition:
- `action`: one of `payments-online`, `lock`, `convert`
- `cartId`: route `{id}`
- `userId`: authenticated principal ID
- `payload`: canonical JSON of the command body (if any), using stable ordering and excluding volatile fields
- `version`: optional static string (e.g., `v1`) to allow future evolution

Computation:
- Use `System.Text.Json` with deterministic settings to serialize payload.
- Concatenate fields with a delimiter and compute SHA‑256; store Base64.

Rules:
- If the same key is reused with a different fingerprint → `409 Conflict` to protect against accidental misuse.
- If header absent → no fingerprint; behavior disabled; handler still expected to be domain‑idempotent.

## Handler‑Level Idempotency Requirements

1) InitiateMemberOnlinePayment
- Before creating a Stripe PaymentIntent, check if the member already has an active (not failed/canceled) intent reference in cart VM/state; if present, return it.
- On creation, pass `Idempotency-Key` to Stripe request options; add metadata: `teamcart_id`, `member_user_id`, `client_idempotency_key`, `request_fingerprint`.
- On webhook `payment_intent.succeeded`/`payment_failed`, update VM using existing Redis store methods so status transitions are monotonic; ensure duplicate webhooks remain no‑ops.

2) Lock TeamCart
- If cart is already locked, return success (or a documented problem code that still maps to 200 for idempotent replay) and do not emit duplicate events.

3) ConvertTeamCartToOrder
- If already converted, return the existing `orderId` deterministically.
- Persist a durable mapping `TeamCartId → OrderId` (DB) so replays and late retries are consistent even if Redis keys expire. If such a mapping already exists in the conversion handler, surface it early.
- Ensure conversion is wrapped in a transaction so we don’t partially convert.

## Web Layer Changes (Minimal APIs)

`src/Web/Endpoints/TeamCarts.cs`:
- Read `Idempotency-Key` header for the three endpoints.
- Option A (recommended): Register `IdempotencyBehavior` and mark the three commands as idempotent. Endpoints remain unchanged aside from DI registration.
- Option B: Add `IdempotencyEndpointFilter` to the three MapPost handlers if behavior approach is not feasible for `IResult` responses.

`src/Web/DependencyInjection.cs`:
- Register `IIdempotencyStore` → `RedisIdempotencyStore`.
- Configure Redis database/key prefix for idempotency.
- Add options: pending TTL (default 60s), completed TTLs (payments: 2h; lock/convert: 48h), and behavior enable/disable per endpoint.

## Error Codes and Semantics

Add or reuse problem codes:
- `Idempotency.MismatchedFingerprint` → 409
- `Idempotency.InFlight` → 202 (or 409) with `Retry-After`
- `TeamCart.AlreadyLocked` → 200 under idempotent replay context
- `TeamCart.AlreadyConverted` → 200 with existing `orderId`

Document them in the TeamCart error catalog and client integration guide.

## Observability

- Logs: Include `idempotency_key`, `fingerprint`, `teamcart_id`, `member_user_id`, and `action` on start, replay, complete, and conflict.
- Metrics: counters for idempotency hits, misses, pending conflicts; Stripe PaymentIntent creations; Redis errors.
- Tracing: span attributes mirroring the above; annotate replays to improve latency dashboards.

## Security Considerations

- Treat `Idempotency-Key` as untrusted input; validate length and characters.
- Scope includes `userId` so one user cannot replay another user’s key.
- Short TTLs for responses containing secrets (Stripe client secret). Consider redacting secrets from stored body and reconstruct on replay from domain state when feasible.
- Rate‑limit sensitive endpoints as per separate recommendation.

## Testing Strategy

Unit tests:
- `RedisIdempotencyStore` start/get/complete semantics, TTL behavior, and fingerprint mismatch handling.
- `IdempotencyBehavior` flow: no header, replay, in‑flight, mismatch.

Integration/E2E:
- Double‑submit `POST /{id}/payments/online` with same key → one PaymentIntent created; second returns same payload.
- Double‑submit `POST /{id}/lock` with same key → returns same success; no duplicate events.
- Double‑submit `POST /{id}/convert` with same key → same `orderId`; only one order in DB.
- Negative: same key with modified body → 409 `MismatchedFingerprint`.
- Crash/resume simulation: after successful conversion, replay with same key returns same `orderId` relying on DB mapping.

## Rollout Plan

1) Implement store + behavior behind feature flag `Idempotency.Enabled`.
2) Wire the three endpoints; initially accept but not require the header. Log warnings for missing header on `payments/online` and `convert` in staging.
3) Staging soak: enable metrics and alerts for mismatches and conflicts.
4) Gradually enforce header requirement for `payments/online` and `convert` for SDK clients; keep `lock` optional.
5) Update client integration docs with examples and error handling guidance.

## Implementation Checklist

- Abstractions
  - [ ] Add `IIdempotencyStore` and DTOs: `IdempotencyRecord`, `IdempotencyCompletedOutcome`, `TryStartResult`.
  - [ ] Add `IIdempotentCommand` marker or `[Idempotent]` attribute with `ActionName`.
- Infrastructure
  - [ ] Implement `RedisIdempotencyStore` with key patterns, TTLs, and JSON serialization.
  - [ ] DI registration and options in `src/Web/DependencyInjection.cs`.
- Application
  - [ ] Add `IdempotencyBehavior<TRequest,TResponse>` using `IHttpContextAccessor` and `IIdempotencyStore`.
  - [ ] Mark commands: `InitiateMemberOnlinePaymentCommand`, `LockTeamCartCommand` (if present), `ConvertTeamCartToOrderCommand`.
  - [ ] Update payment initiation handler to pass Stripe idempotency key and check for existing active intent.
  - [ ] Ensure conversion handler returns existing `orderId` when already converted; persist durable mapping if not already present.
- Web
  - [ ] Optionally add `IdempotencyEndpointFilter` for the three endpoints if behavior cannot capture `IResult` shapes cleanly.
  - [ ] Validate header and emit consistent problems for mismatch/in‑flight states.
- Observability & Docs
  - [ ] Add logs/metrics/traces.
  - [ ] Update TeamCart error codes documentation and client integration guide.

## Open Questions

- Should we enforce 202 vs 409 for in‑flight duplicates? (202 improves UX with retry flow; 409 is simpler but may encourage client backoffs.)
- For responses containing secrets (Stripe client_secret), do we store plaintext in Redis (short TTL) or attempt reconstruction by querying Stripe on replay? (Recommended: short TTL + redact if feasible.)
- Do we need DB‑based idempotency for `convert` instead of Redis? (Recommended: DB mapping for `TeamCartId → OrderId` and Redis for request replay convenience.)

---

Appendix A — Example Fingerprint Inputs

- payments-online:
  - action: `payments-online`
  - cartId: `{id}`
  - userId: `{current_user}`
  - payload: `{}`
  - version: `v1`

- lock:
  - action: `lock`
  - cartId: `{id}`
  - userId: `{current_user}`
  - payload: `{}`
  - version: `v1`

- convert:
  - action: `convert`
  - cartId: `{id}`
  - userId: `{current_user}`
  - payload: `<canonical JSON of ConvertTeamCartRequest>`
  - version: `v1`

Hashing: `SHA256( action + "|" + cartId + "|" + userId + "|" + payload + "|" + version )`

