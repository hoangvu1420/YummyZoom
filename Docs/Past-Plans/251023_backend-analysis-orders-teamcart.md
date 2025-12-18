# Backend Analysis — Orders, Tracking, and TeamCart (MVP Fit + Feasible Upgrades)

Date: 2025-10-23
Author: Backend (reviewing Client App proposals)

Scope
- Review the frontend proposals in 231025_api-completeness-analysis-orders-teamcart.md.
- Cross-check what already exists in code for Orders and TeamCart.
- For each proposal: outline how to implement it in our codebase, provide effort estimates, expected value, and pragmatic workarounds when we defer.

Sources Used (repo paths)
- Orders endpoints: `src/Web/Endpoints/Orders.cs`
- TeamCart endpoints: `src/Web/Endpoints/TeamCarts.cs`
- Real-time hubs: `src/Web/Realtime/Hubs/CustomerOrdersHub.cs`, `RestaurantOrdersHub.cs`, `TeamCartHub.cs`
- TeamCart realtime notifier: `src/Web/Realtime/SignalRTeamCartRealtimeNotifier.cs`
- Order financials: `src/Domain/Services/OrderFinancialService.cs`
- Order initiate flow: `src/Application/Orders/Commands/InitiateOrder/*`
- TeamCart commands: add/update/remove items, lock, payments, convert under `src/Application/TeamCarts/Commands/*`
- Coupons: repo + fast-check: `src/Infrastructure/Persistence/Repositories/CouponRepository.cs`, `src/Application/Coupons/Queries/FastCheck/*`
- Payments (Stripe): `src/Infrastructure/Payments/Stripe/StripeService.cs`, `src/Web/Endpoints/StripeWebhooks.cs`
- Feature flags: `src/Web/Configuration/FeatureFlagsOptions.cs`, `src/Web/Services/TeamCartFeatureAvailability.cs`

—

Current Capabilities (as‑built)
- Orders
  - Create/initiate, accept/reject, cancel, mark preparing/ready/delivered: `POST /api/v1/orders/...` (Orders.cs)
  - Status polling: `GET /api/v1/orders/{orderId}/status`
  - Details/receipt: `GET /api/v1/orders/{orderId}`
  - History: `GET /api/v1/orders/my`
  - Payments: Stripe PaymentIntent creation during initiate via `IPaymentGatewayService`.
- Order Tracking (Real-time)
  - Customer and restaurant SignalR hubs; status broadcasts on lifecycle events.
- TeamCart
  - Create, join, read (SQL view + Redis RT VM), add/update/remove items, apply tip/coupon, lock for payment, member payments (COD/online), convert to order.
  - Real-time notifications over `/hubs/teamcart` (ReceiveCartUpdated/Locked/PaymentEvent/ReadyToConfirm/Converted/Expired).
  - QuoteLite available: per‑member totals with `QuoteVersion`, `GrandTotal` maintained by domain during lock.

—

Proposal-by‑Proposal Analysis

P1.1 Idempotency for Mutations (header: Idempotency-Key)
- What exists
  - Internal idempotency on several command handlers (e.g., re-entrant lifecycle transitions) but no generic HTTP idempotency keyed by header.
- How to implement
  - Add lightweight middleware or endpoint filter that:
    - Reads `Idempotency-Key` (UUIDv4) and a stable request fingerprint (path + method + auth user + normalized body hash).
    - Looks up a durable store; on miss, executes handler, persists outcome snapshot (status + body) with TTL; on hit, returns cached outcome.
  - Storage options:
    - Easiest/fast: Redis (already configured for RT) with `string` key: `idem:{user}:{method}:{path}:{hash}` and JSON payload; TTL 24h.
    - DB table alternative if Redis unavailable in some envs.
  - Integrate on mutating routes only (Orders initiate/cancel; TeamCart add/update/remove/lock/pay/convert).
- Effort
  - S (1–2 dev‑days) for Redis-backed filter + tests; +1 day for DB-backed fallback.
- Value
  - High. Prevents duplicate orders/payments from client retries; reduces operational incidents.
- Workaround if deferred
  - Keep existing handler-level “already in state” checks; document safe retry semantics in each mutation route.

P1.2 Pricing Preview (authoritative)
- What exists
  - Canonical pricing math in `OrderFinancialService` and item snapshot building in InitiateOrder handler.
- How to implement
  - Add `POST /api/v1/pricing/preview` (new `Pricing.cs` group): request `{ restaurantId, items[], couponCode?, tipAmount? }`.
  - Reuse the InitiateOrder item-building path: load menu item + groups/choices, build temporary `OrderItem`s, compute subtotal, fees (placeholder 2.99), tax (placeholder 0–8%), optional coupon via `OrderFinancialService` and `ICouponRepository.GetByCodeAsync`.
  - Response `{ subtotal, discount, deliveryFee, tip, tax, total, currency, notes[] }` where notes include “menu item unavailable”/“prices changed” etc.
- Effort
  - M (2–4 dev‑days) including happy-path tests and basic edge cases.
- Value
  - High. Eliminates front-end drift and improves checkout UX.
- Workaround if deferred
  - Frontend uses `InitiateOrder` dry-run pattern is not available; fallback to TeamCart lock (for groups) for preview; otherwise keep client-side estimates and reconcile on create.

P1.3 Coupon Validation (pre-submit)
- What exists
  - Coupon repository (`GetByCodeAsync`, `GetActiveByRestaurantAsync`) and `FastCouponCheckQuery` that ranks candidates for a given item snapshot.
- How to implement
  - Add `POST /api/v1/coupons/validate` for single-code validation: load coupon by `code + restaurant`, build temporary `OrderItem`s like preview, run `OrderFinancialService.ValidateAndCalculateDiscount`, return `{ valid, discount?, message }`.
  - Optionally expose `POST /api/v1/coupons/fast-check` that returns `FastCouponCheckResponse` for suggestion UI.
- Effort
  - S (1–2 dev‑days) for validate; +1 day for fast-check endpoint.
- Value
  - Medium-High. Improves UX and reduces failed submits.
- Workaround if deferred
  - Validate only during `InitiateOrder`; show errors there.

P2.1 Payments Status Probe
- What exists
  - Stripe integration for PaymentIntent creation and webhooks; no direct probe endpoint.
- How to implement
  - Extend `IPaymentGatewayService` with `Task<Result<PaymentStatusDto>> GetPaymentIntentStatusAsync(string id)` using Stripe `PaymentIntentService.GetAsync`.
  - Add `GET /api/v1/payments/{paymentIntentId}` returning `{ status, amount, currency, lastUpdate }` (maps Processing/Succeeded/Failed/Canceled).
  - Align with webhook flow: probe reflects provider truth; we can include last seen webhook timestamp if present.
- Effort
  - S (1–2 dev‑days).
- Value
  - Medium. Helps resume flows after app interruptions.
- Workaround if deferred
  - Frontend polls order status (for individual orders) or TeamCart VM to infer payment result.

P2.2 TeamCart Batch Item Operations
- Frontend note marked out of scope in MVP for “promote personal cart to TeamCart”.
- What exists
  - Single-item add/update/remove commands with domain validation.
- How to implement (post‑MVP)
  - Add `POST /api/v1/team-carts/{id}/items:batch` that runs ops in a single transaction, short-circuiting on first failure and returning per-op messages.
  - Reuse existing handlers or call domain methods directly inside a dedicated command.
- Effort
  - M (3–5 dev‑days) with validation and partial-failure reporting.
- Value
  - Medium. Useful for migrations; not critical to core flow.
- MVP stance
  - Defer per our prior decision; frontend to perform sequential calls with progress UI.

P2.3 TeamCart Membership Management (leave/kick/transfer-host)
- MVP decision: out of scope.
- What exists
  - Domain tracks `HostUserId`, members, and has relevant error definitions (e.g., `HostCannotLeave`). No leave/kick/transfer methods yet.
- How to implement (post‑MVP)
  - Add domain methods: `Leave(UserId)`, `Kick(HostId, MemberId)`, `TransferHost(HostId, MemberId)` with status/membership guards + events.
  - Endpoints mapping to those commands; broadcast hub events.
- Effort
  - M–L (5–8 dev‑days) including domain, persistence, and RT updates.
- Value
  - Medium. Moderation improves group UX; not required for happy path.
- MVP stance
  - Defer; rely on join + expiration.

P2.4 Share Token Lifecycle (revoke/regenerate)
- MVP decision: out of scope (tokens long‑lived, not revocable).
- What exists
  - `ShareableLinkToken` with `ExpiresAt` and `IsExpired`; generated during create.
- How to implement (post‑MVP)
  - Add domain method `RegenerateToken(HostId, ttl)` and `RevokeToken(HostId)` updating `ShareToken`; emit RT events; persist.
  - Endpoint `POST /api/v1/team-carts/{id}/token` with `{ action }`.
- Effort
  - S–M (2–4 dev‑days).
- Value
  - Medium for security/ops hygiene.
- MVP stance
  - Defer; communicate token semantics to clients.

P3.1 Capabilities Discovery
- MVP note said “treat all as enabled”, but a discovery endpoint is trivial and safe.
- What exists
  - `FeatureFlagsOptions` and `TeamCartFeatureAvailability` (Redis check). Stripe configured in DI.
- How to implement
  - Add `GET /api/v1/capabilities` returning `{ teamCartEnabled, realtimeEnabled, ordersHubEnabled, payments: { provider: 'stripe', applePay: bool, googlePay: bool } }` from config and env checks.
- Effort
  - S (0.5–1 dev‑day).
- Value
  - Low–Medium. Helps client gate features and choose polling vs hub.
- MVP stance
  - Optional; implement if desired for clearer client logic.

P3.2 Quote Versioning for TeamCart (ETag‑like)
- What exists
  - Domain maintains `QuoteVersion`, included in metadata when initiating member payment, but no request-time enforcement.
- How to implement
  - Include `quoteVersion` in responses from lock, and require it in payment and convert commands (header or body). If mismatch, return 409 with latest version.
  - Update handlers: `InitiateMemberOnlinePayment`, `CommitToCodPayment`, `ConvertTeamCartToOrder` to check `request.QuoteVersion == cart.QuoteVersion`.
- Effort
  - S–M (1–3 dev‑days) depending on client plumb‑through.
- Value
  - Medium–High for correctness under concurrency; reduces “paid against stale quote”.
- MVP stance
  - Good candidate to include; small change with real benefit.

P3.3 Standardized Problem Details `code`
- MVP note said to keep current format. Today our `ProblemDetails.Title` carries the error code, but we don’t include a `code` field.
- How to implement
  - Add `problemDetails.Extensions["code"] = error.Code` in `CustomResults.*` so clients can switch on a stable field without parsing `title`.
  - Keep existing RFC7807 fields; fully backward compatible.
- Effort
  - S (0.5 dev‑day) + spot tests.
- Value
  - Medium. Simplifies client error handling.
- MVP stance
  - Safe, non‑intrusive improvement; can be batched with other small wins.

—

Optional (Nice‑to‑Have) Proposals
- Order Reorder Endpoint
  - Implement `POST /api/v1/orders/{orderId}/reorder` to return a cart snapshot suitable for prefill (items + inferred menu IDs + last address). Does not create anything server-side.
  - Effort: M (3–5 dev‑days). Value: Medium.
  - Workaround: client stores last successful payload locally for quick repeat.

- Address Validation
  - Integration with a validator (libpostal, Google, etc.) or simple normalization rules; return normalized address + warnings.
  - Effort: M–L (4–8 dev‑days) depending on provider. Value: Medium.
  - Workaround: keep current basic address VO validation; let delivery issues be handled by restaurant UI.

- TeamCart Deadlines Management
  - Domain already has `SetDeadline`; add `POST /api/v1/team-carts/{id}/deadline { newDeadlineUtc }` guarded to host + status. Optional explicit expire endpoint.
  - Effort: S (1–2 dev‑days). Value: Medium.

- Orders Hub Enhancement (rating prompt)
  - Add an extra broadcast when delivered: `ReceiveOrderDeliveredWithRatingPrompt` piggybacking existing event handler.
  - Effort: S (0.5–1 dev‑day). Value: Low–Medium.

—

Sizing Key and Prioritized Batch for MVP+1
- S: ≤2 dev‑days; M: 3–5; L: 6–10.
- High-value, low-risk batch (recommended next):
  - P1.1 Idempotency (S)
  - P1.2 Pricing Preview (M)
  - P1.3 Coupon Validate (S)
  - P2.1 Payment Probe (S)
  - P3.2 Quote Versioning (S–M)
  - P3.3 ProblemDetails `code` (S)

Estimated Combined Effort: ~8–12 dev‑days, deliverable in 1–2 sprints with parallelization.

Deferred (per MVP scope)
- P2.2 Batch ops for personal cart promotion, P2.3 membership admin, P2.4 token lifecycle. Revisit post‑MVP.

—

Notes on Workarounds (to keep client unblocked now)
- Use existing TeamCart lock to produce an authoritative quote for group checkout screens.
- For retries, rely on handler-level idempotency semantics (e.g., repeated “mark preparing/ready” are safe no‑ops) until header-based idempotency ships.
- Continue using existing RFC7807 responses; when `code` extension lands, clients can incrementally adopt it.

—

Appendix: Quick Feature Map (where to hook changes)
- New endpoints
  - Pricing: `src/Web/Endpoints/Pricing.cs` (new)
  - Coupons validate / fast-check: `src/Web/Endpoints/Coupons.cs` (new)
  - Payments probe: extend `IPaymentGatewayService` + `src/Web/Endpoints/Payments.cs` (new)
  - Capabilities: `src/Web/Endpoints/Capabilities.cs` (new)
- Cross-cutting
  - Idempotency filter/middleware: `src/Web/Infrastructure/IdempotencyFilter.cs` (new) with Redis or EF store.
  - ProblemDetails `code`: `src/Web/Infrastructure/CustomResults.cs` (augment).
- TeamCart concurrency
  - Extend request DTOs to include `quoteVersion` and enforce in relevant handlers.
