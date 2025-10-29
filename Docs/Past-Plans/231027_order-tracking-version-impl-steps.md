# Per-Order Version Implementation (Option 1)

Date: 2025-10-27
Scope: Add monotonic, per-order version for dedupe/ETag, aligned with current code patterns.

---

Summary
- Add `Orders.Version BIGINT NOT NULL DEFAULT 0` in DB (authoritative).
- Increment `Version += 1` on every user-visible change in the Order aggregate (same transaction).
- Expose `version` in `/api/v1/orders/{id}/status`; ETag = `order-<id>-v<version>`.
- Include `version` in FCM data push `{ orderId, version }` from order event handlers.

---

Why this fits current patterns
- Domain methods already centralize state transitions and set `LastUpdateTimestamp` + raise domain events.
- Outbox interceptor publishes events reliably; handlers read Order post-commit and broadcast via SignalR.
- Adding `Version` is minimally invasive: 1 column + small domain updates + 1 DTO/Query update + push payload.

---

Detailed Steps
- Migration & Mapping
  - map the new property in OrderConfiguration (EF configuration in Infrastructure), then scaffold a normal EF migration. 
  - Add this under “Simple Properties” (keep style consistent with other scalar props): builder.Property(o => o.Version).HasColumnName("Version").IsRequired().HasDefaultValue(0);

- Domain (src/Domain/OrderAggregate/Order.cs)
  - Add `public long Version { get; private set; }`.
  - In methods that change user-visible state (Accept/Reject/Cancel/MarkAsPreparing/ReadyForDelivery/Delivered, PaymentSucceeded/Failed, ETA updates): after applying changes, set `Version++` only if any user-visible field changed.

- Query + DTO (status)
  - Extend `OrderStatusDto` with `long Version`.
  - Update `GetOrderStatusQueryHandler` to select `o."Version"` and map it.
  - Compute ETag in the endpoint from `version`: `order-<id>-v<version>`; honor `If-None-Match` → 304.

- Realtime + Push
  - In each Order event handler, after loading the latest Order, use `order.Version` to send FCM data: `{ orderId, version }` to all active tokens of the customer.
  - Client ignores push where `version <= localVersion`.

- Tests
  - Contract tests: `/status` returns `version`; second call with `If-None-Match` returns 304.
  - Functional: state transition increments version by 1; idempotent replays do not increment.
  - Handlers: stub `IFcmService` to assert payload includes the new `version`.

- Rollout
  - Safe to deploy migration first; code reads default 0 for old rows.
  - Once active, ETag switches to version-backed form; older clients remain compatible.

---

Code Pointers
- Domain aggregate: `src/Domain/OrderAggregate/Order.cs`
- Status DTO/Query: `src/Application/Orders/Queries/Common/OrderDtos.cs`, `src/Application/Orders/Queries/GetOrderStatus/*`
- Orders endpoint: `src/Web/Endpoints/Orders.cs`
- Event handlers: `src/Application/Orders/EventHandlers/*`
- FCM service: `src/Infrastructure/Notifications/Firebase/FcmService.cs`
- Token store: `src/Infrastructure/Persistence/Repositories/UserDeviceSessionRepository.cs`

---

Notes
- Only increment for user-visible changes to avoid noise.
- Either optimistic concurrency (rowversion/concurrency token) or repository-level row lock is acceptable; keep consistent with existing transaction boundaries.
- Optional: keep Redis as cache only (mirror latest version for fast 304), not as the source of truth.
