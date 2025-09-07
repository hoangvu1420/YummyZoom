# Phase 3: TeamCart — Comprehensive Implementation Outline

This document consolidates the requirements, architecture, and concrete implementation plan for Phase 3: TeamCart. It enumerates commands, queries, real-time behaviors, cache/store design, event handlers, background jobs, endpoints, testing, observability, and security considerations. It builds on:

- Domain aggregate and events already implemented under `src/Domain/TeamCartAggregate`.
- Domain conversion service already implemented and tested under `src/Domain/Services/TeamCartConversionService.cs`.
- Cache infrastructure readiness summarized in `Docs/Jot-down.md` and implemented in `src/Infrastructure/Caching/*`.

Goals for Phase 3

- Deliver end-to-end TeamCart lifecycle: Open → Locked → ReadyToConfirm → Converted/Expired.
- Provide low-latency collaborative experience backed by Redis as the authoritative TeamCart real-time store.
- Keep application services cohesive, with robust validations, authz, and idempotency, aligned with existing patterns.

Non-Goals in Phase 3

- Restaurant-side batching/combining multiple team carts into a single restaurant ‘session’ (future scope).
- Deep analytics and search facets for TeamCart usage (future scope).

Domain Readiness (Already Implemented)

- Aggregate: `TeamCart` with lifecycle methods including: add member/item, lock for payment, commit to COD, record online payment result, apply/remove coupon, apply tip, mark converted/expired. See `src/Domain/TeamCartAggregate/TeamCart.cs`.
- Entities: `TeamCartItem`, `TeamCartMember`, `MemberPayment`. See `src/Domain/TeamCartAggregate/Entities/*`.
- Enums: `TeamCartStatus`, `PaymentMethod`, `PaymentStatus`. See `src/Domain/TeamCartAggregate/Enums/*`.
- Errors and rich invariants in `src/Domain/TeamCartAggregate/Errors/TeamCartErrors.cs`.
- Events: Created, ItemAdded, LockedForPayment, MemberCommittedToPayment, OnlinePaymentSucceeded/Failed, ReadyForConfirmation, Converted, Expired. See `src/Domain/TeamCartAggregate/Events/*`.
- Conversion domain service: `TeamCartConversionService` to produce `Order` and finalize `TeamCart`. See `src/Domain/Services/TeamCartConversionService.cs`.

High-Level Architecture Decisions

- Authoritative store for TeamCart real-time view is Redis, not SQL. SQL remains the source of truth for aggregate persistence at milestones (and history), but live collaboration reads/writes operate through a dedicated Redis store with atomic updates.
- Application layer exposes commands/queries (CQRS). Queries use Dapper or cache, commands orchestrate domain + stores + side-effects.
- Domain events feed read models and real-time notifications via outbox/inbox idempotent handlers, consistent with Orders.
- Background job(s) expire abandoned carts and reconcile any dangling cache keys.

1) Application Layer — Commands

Implement MediatR commands and validators. All commands use proper authorization attributes/policies and return `Result` or typed results.

- CreateTeamCartCommand
  - Params: `HostUserId`, `RestaurantId`, `HostName`, optional `Deadline`.
  - Flow: validate restaurant exists; call `TeamCart.Create` → persist; write initial Redis VM; publish Created event.
  - Auth: `[Authorize]` (customer). Validator: host name non-empty; deadline not in past.

- JoinTeamCartCommand
  - Params: `TeamCartId`, `ShareToken`, `GuestUserId`, `GuestName`.
  - Flow: validate token via `teamCart.ValidateJoinToken`; call `AddMember`; persist; upsert Redis; emit MemberJoined (from domain).
  - Auth: `[Authorize]` (customer). Throttle/anti-abuse on token.

- AddItemToTeamCartCommand
  - Params: `TeamCartId`, `UserId`, `MenuItemId`, `Quantity`, `Customizations[]`.
  - Flow: load required MenuItem + CustomizationGroups; validate availability and assignments; enforce per-group cardinality (min/max, duplicates, required groups applied to the item); call aggregate `AddItem`; persist; raise ItemAdded event.
  - Redis VM: Recommended to update the VM in an idempotent domain-event handler for `ItemAddedToTeamCart` (outbox-driven), not inside the command. Handlers upsert the item into Redis and broadcast. This keeps commands transactional on SQL and VM updates resilient and retryable.
  - Auth: member-only; cart must be Open.

- UpdateTeamCartItemQuantityCommand
  - Params: `TeamCartId`, `UserId`, `TeamCartItemId`, `NewQuantity`.
  - Flow: aggregate `UpdateQuantity`; persist; apply Redis mutation atomically.
  - Auth: only owner of item; cart Open.

- RemoveItemFromTeamCartCommand
  - Params: `TeamCartId`, `UserId`, `TeamCartItemId`.
  - Flow: remove by owner; persist; Redis mutation.
  - Auth: owner; cart Open.

- LockTeamCartForPaymentCommand
  - Params: `TeamCartId`, `HostUserId`.
  - Flow: `LockForPayment`; persist; Redis flag change; emit LockedForPayment; notify members.
  - Auth: host.

- ApplyTipToTeamCartCommand
  - Params: `TeamCartId`, `HostUserId`, `TipAmount`.
  - Flow: aggregate `ApplyTip`; persist; Redis mutation.
  - Auth: host; cart Locked.

- ApplyCouponToTeamCartCommand / RemoveCouponFromTeamCartCommand
  - Params: `TeamCartId`, `HostUserId`, `CouponCode` (apply); none for remove.
  - Flow: load coupon; validate eligibility; set aggregate coupon; optionally precompute `discountAmount` for later conversion; persist; Redis mutation; record intended usage for later reconciliation (coupon usage finalize on order success per Phase 1 rules).
  - Auth: host; cart Locked.

- CommitToCodPaymentCommand
  - Params: `TeamCartId`, `UserId`.
  - Flow: compute member subtotal; `CommitToCashOnDelivery(userId, amount)`; persist; Redis mutation; event MemberCommitted; check transition to ReadyToConfirm.
  - Auth: member; cart Locked.

- InitiateMemberOnlinePaymentCommand
  - Params: `TeamCartId`, `UserId`.
  - Flow: compute member subtotal; create PaymentIntent via `IPaymentGatewayService.CreatePaymentIntentAsync` with metadata `{ teamcart_id, member_user_id }`; return client secret to caller. Do NOT mutate aggregate until webhook confirms.
  - Auth: member; cart Locked.

- HandleTeamCartStripeWebhookCommand
  - Trigger: Stripe webhook endpoint detects TeamCart metadata (teamcart_id present and no order_id).
  - Flow: idempotency check; load aggregate; on `payment_intent.succeeded` → `RecordSuccessfulOnlinePayment(userId, amount, transactionId)`; on failed → mark failed; persist; Redis mutation; check transition to ReadyToConfirm; store processed event id.

- ConvertTeamCartToOrderCommand
  - Params: `TeamCartId`, `HostUserId`, `DeliveryAddressDto`, `SpecialInstructions`.
  - Flow: validate `ReadyToConfirm`; compute delivery fee/tax; validate coupon discount; call `TeamCartConversionService.ConvertToOrder`; persist both aggregates in one transaction; emit `TeamCartConverted`.
  - Auth: host.

- ExpireTeamCartsCommand (internal)
  - Triggered by scheduler. Marks eligible carts as expired (domain `MarkAsExpired`) and clears Redis keys.

2) Application Layer — Queries

- GetTeamCartDetailsQuery (SQL)
  - Params: `TeamCartId`.
  - Handler: Dapper multi-join to `TeamCarts` + owned collections (`TeamCartMembers`, `TeamCartItems`, `TeamCartMemberPayments`). Optimized for a single cart by id.
  - Auth: member or host. Response: detailed DTO for fallback or initial load.

- GetTeamCartRealTimeViewModelQuery (Redis-only)
  - Params: `TeamCartId`.
  - Handler: Reads the denormalized VM from Redis via `ITeamCartStore`; never hits SQL. This powers live updates.
  - Auth: member or host. Response: `TeamCartViewModel`.

- FindTeamCartByTokenQuery (SQL)
  - Params: share token string.
  - Handler: SQL lookup to resolve `TeamCartId` + minimal info; used to show “join” screen.
  - Auth: anonymous allowed to resolve for the join view; actions require auth.

3) Application Layer — New Interfaces

- ITeamCartRepository (EF-backed)
  - Methods: `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `GetOpenOrLockedByDeadlineRangeAsync` (for expiration scan), etc.

- ITeamCartStore (Redis-backed authoritative team cart store)
  - Methods (atomic): `CreateVm(...)`, `AddMember(...)`, `AddItem(...)`, `UpdateItemQty(...)`, `RemoveItem(...)`, `SetLocked(...)`, `ApplyTip(...)`, `ApplyCoupon(...)`, `CommitCod(...)`, `RecordOnlinePayment(...)`, `SetReadyToConfirm(...)` (if needed), `DeleteVm(...)`, `GetVm(...)`.
  - Semantics: all write ops must be atomic and refresh TTL; publish update notifications.

- ITeamCartRealtimeNotifier (SignalR-backed in Web host, NoOp in Infrastructure by default)
  - Methods: `NotifyCartUpdated(cartId)`, `NotifyPaymentEvent(cartId, userId, status)`, `NotifyLocked`, `NotifyReadyToConfirm`, `NotifyConverted`, `NotifyExpired`.

4) Infrastructure — Persistence (EF)

- Add TeamCartRepository implementing ITeamCartRepository using EF `ApplicationDbContext`.
- Ensure owned collections mapping aligns with snapshots (already scaffolded by migrations) and field-backed collections remain encapsulated.

5) Infrastructure — Redis TeamCart Store

- Implement `RedisTeamCartStore : ITeamCartStore` using `IConnectionMultiplexer`.
- Keying
  - VM key: `yz:{env}:teamcart:vm:{teamCartId}:v1`.
  - Optional group/tag keys for invalidation: `teamcart:{id}`.
- Data layout
  - Prefer Redis hash for top-level fields (status, tip, coupon, totals) + nested JSON for items/members to balance partial updates and simplicity, or keep entire JSON blob initially and evolve to hashes as needed.
  - Maintain a version/ETag field incremented on each write for optimistic client-side refresh.
- TTL
  - Sliding TTL (e.g., 30–60 minutes) refreshed on read/write; absolute expiry on `Converted|Expired` (delete key).
- Atomicity
  - Use Lua scripts or `WATCH/MULTI/EXEC` to perform compound validations and updates (e.g., add item updates totals; lock checks status and enforces not-empty; payments update and check readiness). Provide a small set of reusable scripts.
- Notifications
  - Publish `teamcart:updates` channel messages `{ cartId, updateType, version }`. The Web host subscriber can fan-out via SignalR to `teamcart:{id}` group.
- Observability
  - Counters: ops/sec, conflicts, script failures, expirations. Structured logs with `cartId` and `op`.

6) Infrastructure — Cache + Eventing Integration

- Reuse existing cache invalidation pub/sub for tag-based invalidations where needed (not primary for TeamCart VM).
- Ensure `CacheInvalidationSubscriber` remains enabled in tests and runtime.

7) Domain Event Handlers (Inbox/Outbox)

Implement idempotent handlers in Application for TeamCart events to update Redis VM and/or push real-time notifications:

- TeamCartCreated → create initial VM.
- ItemAddedToTeamCart → upsert member/items in VM; broadcast.
- TeamCartLockedForPayment → set status; broadcast.
- MemberCommittedToPayment → update payment status; recompute readiness; broadcast.
- OnlinePaymentSucceeded/Failed → update payment; broadcast; on transition to ReadyToConfirm → broadcast.
- TeamCartReadyForConfirmation → set ready flag; broadcast.
- TeamCartConverted → delete VM; broadcast conversion.
- TeamCartExpired → delete VM; broadcast expiration.

Pattern mirrors existing order handlers in `src/Application/Orders/EventHandlers/*` using base `IdempotentNotificationHandler` and `IInboxStore`.

8) Background Processing — Expiration

- TeamCartExpirationHostedService (BackgroundService)
  - Periodically queries for carts past `ExpiresAt` and not in terminal state.
  - For each: call aggregate `MarkAsExpired`, persist via repository, publish `TeamCartExpired` (domain event), delete Redis VM.
  - Configurable cadence and batch size via appsettings (e.g., every 1–5 minutes, batch 200).
- Optional: wire Redis keyspace notifications to detect TTL-based expirations if we set absolute TTL on VM keys; still rely on DB scan as the source of truth.

9) Web (API Endpoints) — Minimal APIs

Add a new `TeamCarts` endpoint group under `src/Web/Endpoints/TeamCarts.cs` (v1). Examples:

- POST `/api/v1/teamcarts` → CreateTeamCart
- POST `/api/v1/teamcarts/{id}/join` → JoinTeamCart
- POST `/api/v1/teamcarts/{id}/items` → AddItemToTeamCart
- PATCH `/api/v1/teamcarts/{id}/items/{itemId}` → UpdateTeamCartItemQuantity
- DELETE `/api/v1/teamcarts/{id}/items/{itemId}` → RemoveItemFromTeamCart
- POST `/api/v1/teamcarts/{id}/lock` → LockTeamCartForPayment
- POST `/api/v1/teamcarts/{id}/tip` → ApplyTipToTeamCart
- POST `/api/v1/teamcarts/{id}/coupon` → ApplyCouponToTeamCart
- DELETE `/api/v1/teamcarts/{id}/coupon` → RemoveCouponFromTeamCart
- POST `/api/v1/teamcarts/{id}/payments/cod` → CommitToCodPayment
- POST `/api/v1/teamcarts/{id}/payments/online` → InitiateMemberOnlinePayment
- POST `/api/v1/teamcarts/{id}/convert` → ConvertTeamCartToOrder
- GET `/api/v1/teamcarts/{id}` → GetTeamCartDetails (SQL)
- GET `/api/v1/teamcarts/{id}/rt` → GetTeamCartRealTimeViewModel (Redis-only)

10) Web (SignalR) — Real-time Collaboration

- Add `TeamCartHub` with groups per cart `teamcart:{id}`.
  - Methods: `SubscribeToCart(cartId)` and `UnsubscribeFromCart(cartId)`, with authorization requiring member/host.
  - Broadcast methods: `ReceiveCartUpdated`, `ReceivePaymentEvent`, `ReceiveLocked`, `ReceiveReadyToConfirm`, `ReceiveConverted`, `ReceiveExpired`.
- Add `ITeamCartRealtimeNotifier` implementation in Web host, similar to `SignalROrderRealtimeNotifier`, to publish to the `teamcart:{id}` group.

11) Webhooks — Stripe Integration for Member Payments

- Extend existing Stripe webhook handling to route TeamCart-member PaymentIntent events:
  - Detect `teamcart_id` and `member_user_id` in metadata (no `order_id`).
  - Dispatch to `HandleTeamCartStripeWebhookCommand`.
  - Record processed event id in `ProcessedWebhookEvents` for idempotency.

12) Real-Time View Model Shape (first cut)

- TeamCartViewModel
  - `CartId`, `RestaurantId`, `Status`, `Deadline`, `ExpiresAt`, `ShareTokenMasked`
  - `Members[]`: `UserId`, `Name`, `Role`, `PaymentStatus`, `CommittedAmount`, `OnlineTransactionId?`
  - `Items[]`: `ItemId`, `AddedByUserId`, `Name`, `Quantity`, `BasePrice`, `LineTotal`, `Customizations[]`
  - `Financials`: `Subtotal`, `TipAmount`, `CouponCode?`, `DiscountAmount`, `DeliveryFee`, `TaxAmount`, `Total`, `CashOnDeliveryPortion`
  - `Version` (ETag)

13) Validation & Authorization

- Validators (FluentValidation) for all commands: quantities > 0, customizations match allowed groups, deadline/tip formats, coupon existence.
- Policies:
  - `MustBeTeamCartMember` for read and member actions.
  - `MustBeTeamCartHost` for lock/financials/convert.
  - Enforce restaurant scoping consistency (items and actions must match cart’s restaurant).
- Rate limiting on join and item mutation endpoints to mitigate abuse.

14) Testing Strategy

- Unit tests: aggregate behaviors largely covered; add tests for conversion edge cases if needed.
- Functional tests (Application): command handlers happy paths and failures for each command; authorization failures; idempotency checks.
- Integration tests: `RedisTeamCartStore` atomic mutations, TTL sliding, cross-factory coherence (two app factories observing same cart VM via Redis Testcontainer).
- Webhook: end-to-end — initiate PaymentIntent with metadata, simulate Stripe webhook JSON, verify member payment state and ReadyToConfirm transition.
- Background: expiration hosted service marks DB + deletes Redis key; verify `TeamCartExpired` event handling.

15) Observability & Ops

- Structured logging for all TeamCart commands with `CartId`, `RestaurantId`, `ActingUserId`, and operation names.
- Metrics: cart creations, joins, item mutations, locks, payments committed (COD/online), conversions, expirations, Redis conflicts.
- Tracing: tag spans with `teamcart_id` and Redis script names.

16) Security & Privacy

- Share token format is unguessable; joining requires possession of token and authentication.
- Do not leak member PII to non-members; API returns masked token.
- Validate that menu items/customizations belong to the same restaurant and are currently available.
- Webhook signature verification already present for Stripe; extend to TeamCart path.

17) Rollout Plan

- Feature flag: `Features:TeamCart` controls endpoints and hub registration.
- Soft-launch: enable in staging with Redis Testcontainer; verify real-time and webhook processing.
- Production: enable Redis connection via Aspire or `Cache:Redis:ConnectionString`; monitor metrics; progressively roll out to percentage of users if needed at API gateway.

18) Concrete Work Items (Backlog)

- Application
  - Define DTOs and commands listed above; add validators and policies.
  - Add `ITeamCartRepository`, `ITeamCartStore`, `ITeamCartRealtimeNotifier` interfaces.
  - Implement command/handler orchestration including coupon validation and payment initiation.
  - Add event handlers for TeamCart domain events (idempotent via Inbox).
  - Add queries: SQL details and Redis real-time VM.

- Infrastructure
  - Implement EF `TeamCartRepository` and DI registration.
  - Implement `RedisTeamCartStore` with atomic scripts, TTL, pub/sub updates; DI registration.
  - Add `TeamCartExpirationHostedService` and configuration.
  - Extend Stripe webhook endpoint/handler routing to TeamCart member payments.
  - Add NoOp implementation of `ITeamCartRealtimeNotifier` in Infrastructure.

- Web
  - Add `TeamCarts` endpoint group with routes above.
  - Add `TeamCartHub` and `SignalRTeamCartRealtimeNotifier` in Web host; wire hub in `Program.cs`.
  - Consider CORS and connection limits for real-time traffic.

- Tests
  - Add functional tests for commands and queries.
  - Add integration tests for Redis store and webhook flow.
  - Add background service tests for expiration.

19) Known Gaps / Decisions to Confirm

- Online payments per member: initial approach uses a PaymentIntent per member with metadata. Confirm whether to allow partial online + COD mix; the domain supports mixed methods already.
- Coupon application: discount computation occurs in Application; conversion revalidates totals. Confirm whether to pre-authorize coupon usage on lock or on conversion (recommended: finalize on order success as in Phase 1).
- Real-time VM schema: start with flat JSON blob for simplicity, evolve to hashes if/when hotspots identified.
- Keyspace notifications: optional; rely on DB-driven expiration for correctness.

Appendix — Reference Files (for current code)

- Domain aggregate and entities: `src/Domain/TeamCartAggregate/TeamCart.cs`
- Domain events: `src/Domain/TeamCartAggregate/Events/TeamCartReadyForConfirmation.cs`
- Domain conversion service: `src/Domain/Services/TeamCartConversionService.cs`
- Cache infra and pub/sub: `src/Infrastructure/Caching/DistributedCacheService.cs`, `src/Infrastructure/Caching/RedisInvalidationPublisher.cs`, `src/Infrastructure/Caching/CacheInvalidationSubscriber.cs`
- Outbox/Inbox patterns & order event handlers (reference pattern): `src/Application/Orders/EventHandlers/OrderPlacedEventHandler.cs`
- Stripe webhook (order path): `src/Application/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs`
- Web SignalR pattern for orders: `src/Web/Realtime/SignalROrderRealtimeNotifier.cs`, `src/Web/Realtime/Hubs/CustomerOrdersHub.cs`, `src/Web/Realtime/Hubs/RestaurantOrdersHub.cs`

## Phased Implementation Checklist (Logical Sequence)

Decision: Perform TeamCart VM mutations in idempotent domain-event handlers (outbox/inbox) for consistency and retries. Commands focus on aggregate changes and persistence; handlers update Redis VM and broadcast real-time updates.

Phase 3.0 — Foundations & Flags

- [x] Add feature flag `Features:TeamCart` (default off in prod, on in dev/test).
- [x] Add config for Redis TeamCart store: key prefix, TTL, channel, max items.
- [x] Add DI placeholders and guard rails when Redis is not configured (clear warning logs, availability service to control endpoint mapping).

Phase 3.1 — Contracts & DI Wiring

- [x] Define `ITeamCartRepository` in Application (get/add/update/find for expiration).
- [x] Define `ITeamCartStore` in Application (atomic ops + `GetVm`, `DeleteVm`).
- [x] Define `ITeamCartRealtimeNotifier` in Application.
- [x] Add `TeamCartViewModel` DTO for real-time store and queries.
- [x] Infrastructure: register EF `TeamCartRepository` and NoOp `TeamCartRealtimeNotifier`.
- [x] Web host: register SignalR-backed `TeamCartRealtimeNotifier` and hub routing (behind flag).

Phase 3.2 — Redis Store (Minimal Viable)

- [x] Implement `RedisTeamCartStore` with: `CreateVm`, `GetVm`, `DeleteVm`.
- [x] Add version/ETag field and sliding TTL refresh on read/write.
- [x] Implement update ops: `AddMember`, `AddItem`, `UpdateItemQty`, `RemoveItem`, `SetLocked`, `ApplyTip`, `ApplyCoupon`, `RemoveCoupon`, `CommitCod`, `RecordOnlinePayment`.
- [x] Publish `teamcart:updates` messages with `{cartId, type}` (MVP — can include `version` later).
- [x] Add optimistic concurrency via conditional transaction (WATCH/MULTI/EXEC semantics) with retries and jittered backoff.
  - Current: compare-and-set on the JSON value using StackExchange.Redis `Condition.StringEqual` + `StringSet` in a transaction.
  - Next: move to Lua CAS and/or field-level Hash updates for hotspots.

Phase 3.3 — Open Phase Commands

- [x] CreateTeamCartCommand (+ validator, auth). Persist aggregate only.
- [x] JoinTeamCartCommand (+ validator, auth). Persist aggregate only.
- [x] AddItemToTeamCartCommand (+ validator, auth). Persist aggregate only.
- [x] UpdateTeamCartItemQuantityCommand (+ validator, auth). Persist aggregate only.
- [x] RemoveItemFromTeamCartCommand (+ validator, auth). Persist aggregate only.
- [x] Tests: happy paths, invalid token, non-member add, item validations (availability, customization groups).

Phase 3.4 — Event Handlers (Open Phase)

- [x] TeamCartCreated handler → `ITeamCartStore.CreateVm`, broadcast `NotifyCartUpdated`.
- [x] MemberJoined handler → store `AddMember`, broadcast.
- [x] ItemAddedToTeamCart handler → store `AddItem`, broadcast.
- [x] ItemRemovedFromTeamCart handler → store `RemoveItem`, broadcast.
- [x] ItemQuantityUpdatedInTeamCart handler → store `UpdateItemQty`, broadcast.
- [x] Tests: inbox idempotency, duplicate events no-op, post-mutation VM shape and data assertions.

Phase 3.5 — Lock & Financials

- [x] Commands (persist aggregate only; add validators + auth):
  - LockTeamCartForPaymentCommand (host-only).
  - ApplyTipToTeamCartCommand (host-only, Locked; validate tip ≥ 0; use domain currency).
  - ApplyCouponToTeamCartCommand (host-only, Locked; resolve `CouponCode` via `ICouponRepository`; perform early validations for enabled/valid window/scope; do not increment usage here).
  - RemoveCouponFromTeamCartCommand (host-only, Locked).
- [ ] Domain events (financials) to keep VM updates outbox-driven (consistent with earlier phases):
  - TipAppliedToTeamCart(TeamCartId, Money)
  - CouponAppliedToTeamCart(TeamCartId, CouponId)
  - CouponRemovedFromTeamCart(TeamCartId)
- [ ] Handlers (idempotent via inbox):
  - TeamCartLockedForPayment → `ITeamCartStore.SetLockedAsync` then `NotifyLocked` + `NotifyCartUpdated`.
  - TipAppliedToTeamCart → store `ApplyTipAsync` then broadcast.
  - CouponAppliedToTeamCart → store `ApplyCouponAsync` (discount amount may be 0 or a lightweight estimate); broadcast.
  - CouponRemovedFromTeamCart → store `RemoveCouponAsync`; broadcast.
- [ ] Implementation notes:
  - Do not mutate Redis from commands; rely on outbox-driven handlers after successful `SaveChanges`.
  - Coupon discount in VM is informational; either set to 0 or compute a lightweight estimate. Final discount is computed during conversion (Phase 3.7).
  - Keep currency consistency by sourcing from domain `Money` values.
  - Add structured logs on commands/handlers with `cartId`, `actingUserId`, `eventId`.
- [ ] Tests:
  - Lock: cannot lock empty, only host can lock, VM status becomes Locked, notifier called once (idempotent on re-drain).
  - Tip: only when Locked, negative rejected, VM tip updated.
  - Coupon: only when Locked, apply-once enforcement, remove works and is idempotent, VM reflects coupon/discount.
  - Handlers idempotency: re-drain outbox does not duplicate store mutations or notifications.

Phase 3.6 — Member Payments

- [ ] CommitToCodPaymentCommand (member). Persist aggregate only.
- [ ] InitiateMemberOnlinePaymentCommand → calls `IPaymentGatewayService.CreatePaymentIntentAsync` with `{ teamcart_id, member_user_id }` metadata; returns client-secret; no aggregate mutation.
- [ ] New `HandleTeamCartStripeWebhookCommand`: on payment succeeded/failed, mutate aggregate (record online payment result) with idempotency and then rely on event handlers to update VM.
- [ ] Handlers: MemberCommittedToPayment, OnlinePaymentSucceeded/Failed → update payment in store; recompute readiness; broadcast.
- [ ] Tests: payment flows, idempotent webhook processing, mixed COD+Online.

Phase 3.7 — ReadyToConfirm & Conversion

- [ ] Handler: TeamCartReadyForConfirmation → `NotifyReadyToConfirm` + store update if separate flag used.
- [ ] ConvertTeamCartToOrderCommand → validate host + ReadyToConfirm; compute delivery fee/tax/discount; call `TeamCartConversionService`; persist order + updated cart in single transaction.
- [ ] Handler: TeamCartConverted → delete VM; broadcast `NotifyConverted`.
- [ ] Tests: conversion success, mismatch validations, coupon reconciliation, order linkage (`SourceTeamCartId`).

Phase 3.8 — Expiration

- [ ] Implement `TeamCartExpirationHostedService` with cadence, batch, and backoff settings.
- [ ] ExpireTeamCartsCommand (internal) if a command abstraction is desired for testability.
- [ ] Aggregate `MarkAsExpired` persisted; handler: TeamCartExpired → delete VM; broadcast `NotifyExpired`.
- [ ] Tests: expiration service marks DB, deletes VM, idempotent re-runs.

Phase 3.9 — Web Endpoints & SignalR

- [ ] Endpoint group `TeamCarts` mapping all listed routes; enforce policies.
- [ ] Conditionally map the `TeamCarts` endpoint group based on `ITeamCartFeatureAvailability` (disabled when `Features:TeamCart` is false or Redis is not configured). Option: return a consistent 503/feature-disabled response if called.
- [ ] `TeamCartHub` with `SubscribeToCart/UnsubscribeFromCart` (auth: member/host); group `teamcart:{id}`.
- [ ] `SignalRTeamCartRealtimeNotifier` implementation; integrate with handlers.
- [ ] Tests: endpoint authZ, hub subscription authZ, basic smoke for broadcasts.

Phase 3.10 — Observability, Limits, Hardening

- [ ] Structured logging on commands + handlers with `cartId`, `restaurantId`, `actingUserId`.
- [ ] Metrics counters/timers for Redis ops, conflicts, expirations, conversion latency.
- [ ] Rate limiting on join and mutation endpoints; input size guards (max items per cart).
- [ ] Security review: share token entropy, leakage checks, webhook signature path for TeamCart.

Phase 3.11 — Rollout

- [ ] Staging enablement with Redis Testcontainer; execute playbook: create → join → mutate → lock → pay (online+COD) → convert.
- [ ] Production readiness checklist: Redis connectivity, TTLs, dashboards, alerts, webhook secrets.
- [ ] Gradual enable via feature flag; monitor metrics and logs; expand to 100%.
