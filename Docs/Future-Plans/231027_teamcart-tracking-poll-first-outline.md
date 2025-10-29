# TeamCart Tracking — Poll‑First with FCM Triggers (MVP)

Date: 2025-10-27
Scope: Deliver client‑app poll‑first tracking for TeamCart using the Redis real‑time endpoint (`/rt`) with conditional responses, plus FCM data messages to trigger immediate polls. Keep existing SignalR hub as a complementary channel.

---

Objectives
- Single source of truth for tracking (MVP): GET `/api/v1/team-carts/{id}/rt` with strong ETag/304 for cheap polling and fetch‑on‑FCM.
- Push‑assisted polling: Send FCM data `{ teamCartId, version }` on every user‑visible change; client polls `/rt` to reconcile.
- Efficient polling: 304 Not Modified when unchanged using strong ETag based on a monotonic `Version`.
- Backward compatible: No breaking change to current TeamCart mutations or SignalR events; hub remains for web.
- Safety: Idempotent writes; continue using `quoteVersion` for optimistic concurrency on financial operations.

---

Client Requirements Recap (aligned with Orders MVP)
- Clients poll the tracking endpoint frequently while a TeamCart is active; polling backs off when backgrounded.
- FCM data‑only pushes are used to nudge clients to poll immediately after changes; clients dedupe by `version`.
- Tracking endpoint responds with ETag and Last‑Modified; clients send `If-None-Match` to minimize payloads.
- Terminal states (`Converted`, `Expired`) stop polling except for rare resume checks.

---

Current Capabilities (as‑is)
- Endpoints
  - `GET /api/v1/team-carts/{id}` — SQL details (full view).
  - `GET /api/v1/team-carts/{id}/rt` — Redis real‑time view model (VM) with `Version` and `QuoteVersion`.
  - Mutations: add/update/remove item, apply/remove coupon, apply tip, lock, member payments (COD/online), convert, etc. Some accept `quoteVersion`.
- Real‑time hub
  - `TeamCartHub` group `teamcart:{id}` emits: `ReceiveCartUpdated`, `ReceiveLocked`, `ReceivePaymentEvent`, `ReceiveReadyToConfirm`, `ReceiveConverted`, `ReceiveExpired`.
- Domain and VM
  - Domain `QuoteVersion (long)` increments on quote recomputation (primarily while Locked/financials).
  - Redis TeamCart VM maintains a monotonic `Version` that increments on every VM mutation (members/items/coupon/tip/status/payments/quote update).

Gap: Using `QuoteVersion` as an HTTP validator misses non‑quote changes. For poll‑first correctness, use the VM `Version` as the change token.

---

Proposed Contracts (Poll‑First + Push Triggers)
1) Tracking endpoint (MVP: reuse `/rt`)
- Route: `GET /api/v1/team-carts/{id}/rt` (existing).
- Add strong ETag/304 behavior:
  - Response 200 JSON: current VM as‑is, including `Version`, `QuoteVersion`, `Status`, `Members`, `Items`, etc.
  - Headers on 200:
    - `ETag: "teamcart-<id>-v<Version>"` (strong)
    - `Last-Modified: <server timestamp>`
    - `Cache-Control: no-cache, must-revalidate`
  - Conditional: If `If-None-Match` matches → `304 Not Modified` with empty body.
- Optional: `HEAD /api/v1/team-carts/{id}/rt` returns only headers to check freshness.
- Near‑term (optional): Introduce a thin `/status` projection that returns only `{ teamCartId, status, version, quoteVersion, small counters }`, keeping the same ETag format for drop‑in replacement.

2) FCM data message (push trigger)
- Payload: `{ "teamCartId": "<id>", "version": <long> }`
- Targeting: all active device tokens of current cart members (host + participants).
- Platform hints (same as Orders MVP):
  - Android: `priority=high`, `collapse_key="teamcart_<id>"`, `time_to_live≈300s`
  - iOS/APNs: `apns-push-type=background`, `content-available=1`, `apns-collapse-id=teamcart_<id>`
- Reliability: use Outbox‑driven dispatcher; idempotent sends; retries on transient failures.

Notes
- Keep SignalR broadcasts unchanged for web. Mobile clients may also subscribe but do not rely on it for correctness.

---

Client Behavior (reference)
- On app foreground or screen open: poll `/rt` immediately; then poll every 10–15s while active.
- On receiving FCM `{teamCartId, version}`:
  - If `version` ≤ local, ignore.
  - Else, call `/rt` with `If-None-Match`. If 200, update local snapshot and UI; if 304, keep current.
- Stop polling on `Converted` or `Expired`; optionally schedule a final grace poll on resume.

---

Acceptance Criteria (MVP)
- `GET /api/v1/team-carts/{id}/rt` returns 200 with strong ETag based on VM `Version`, and 304 on `If-None-Match` match.
- (Optional) `HEAD /api/v1/team-carts/{id}/rt` returns headers only.
- FCM data‑only pushes are emitted for each user‑visible TeamCart change with correct `{teamCartId, version}`.
- No changes to existing mutation routes or SignalR contracts.
- Contract tests cover 200 + headers, 304, 401/404.

---

Design Details and Rationale
- Why VM.Version (not QuoteVersion): Quote recomputation doesn’t happen for all interactions (join/leave, item edits before lock, coupon/tip toggles). VM `Version` increments on each mutation in `RedisTeamCartStore.MutateAsync(...)`, so it is a reliable validator and push dedupe token.
- Source of truth tradeoff: Tracking via `/rt` returns authoritative tracking fields from the VM. Detailed SQL remains at `GET /team-carts/{id}` for the full view.
- Security: Hide existence when unauthorized (404). Push payload contains no PII.

---

Code Pointers
- Endpoint group: `src/Web/Endpoints/TeamCarts.cs` (add ETag/304 and optional HEAD to `GET /{id}/rt`).
- VM access: `src/Infrastructure/StateStores/TeamCartStore/RedisTeamCartStore.cs` and `src/Application/Common/Interfaces/IServices/ITeamCartStore.cs`.
- Domain events and hub: `src/Web/Realtime/Hubs/TeamCartHub.cs`, `src/Web/Realtime/SignalRTeamCartRealtimeNotifier.cs`.
- FCM service: `src/Application/Common/Interfaces/IServices/IFcmService.cs`, impl `src/Infrastructure/Notifications/Firebase/FcmService.cs`.
- Device tokens: `src/Infrastructure/Persistence/Repositories/UserDeviceSessionRepository.cs`.
- Tests: add under `tests/Web.ApiContractTests/TeamCarts/` similar to Orders status tests.

---

Activity Messages (Option A — Client‑Side Diff)
- Goal: support user‑friendly activity like “Alice joined”, “Bob added Burger x2”, without adding new backend feeds.
- Approach: client diffs the Redis VM snapshot to derive messages.
  - Trigger: on FCM `{teamCartId, version}` or `/rt` 200, request `GET /api/v1/team-carts/{id}/rt` with `If-None-Match`.
  - If 304: no changes → no messages.
  - If 200: compare new VM vs last local snapshot and emit messages:
    - Members: new `member.userId` → “{name} joined”; missing → “{name} left”.
    - Items: new item → “{actorName} added {itemName} x{qty}”.
    - Item quantity: qty increase/decrease → “{actorName} updated {itemName} to x{qty}”.
    - Item removed → “{actorName} removed {itemName}”.
    - Coupon/Tip: field change → “Host applied/removed coupon {code}”, “Host set tip to {amount}”.
    - Status transitions: `Locked/ReadyToConfirm/Converted/Expired` → corresponding banner/notification.
    - Payments: `CommittedAmount/PaymentStatus/OnlineTransactionId` changes per member → COD committed / online paid / payment failed.
- Server tweak required: add strong ETag to `GET /api/v1/team-carts/{id}/rt` based on VM `Version`; return 304 on `If-None-Match` match.
- Data sources used for diff:
  - Members: `TeamCartViewModel.Members[{ userId, name, paymentStatus, committedAmount, onlineTransactionId, quotedAmount }]`.
  - Items: `Items[{ itemId, addedByUserId, name, quantity, lineTotal }]`.
  - Financial toggles: `CouponCode`, `DiscountAmount`, `TipAmount`.
  - Cart status: `Status`, plus `QuoteVersion` for financial recalculations.
- Client rendering notes:
  - Use on‑device i18n templates; resolve names from VM (no PII in FCM).
  - Keep a rolling local snapshot keyed by `teamCartId`; store last ETag and `version`.
  - Coalesce bursts by `version` if multiple messages map to the same VM change.
- Acceptance (Option A add‑on):
  - `GET /api/v1/team-carts/{id}/rt` returns strong ETag = `"teamcart-<id>-v<version>"` and 304 on match.
  - Documented client diff rules for Members/Items/Coupon/Tip/Status/Payments.
  - No new backend persistence; only HTTP validator support added.

Code pointers (Option A)
- VM endpoint: `src/Web/Endpoints/TeamCarts.cs` (GET `/{id}/rt`) — add ETag/304.
- VM model: `src/Application/TeamCarts/Models/TeamCartViewModel.cs` — fields used by diff.
- VM store: `src/Infrastructure/StateStores/TeamCartStore/RedisTeamCartStore.cs` — `Version` increments on every mutation.

---

Sequential Implementation Plan

1) Add ETag + conditional handling on `/rt`
- Compute strong ETag: `"teamcart-<id>-v<Version>"` and set `Last-Modified` (use server now or VM touch timestamp; ETag is the validator of record).
- If `If-None-Match` equals current → return 304.
- (Optional) Add `HEAD /{id}/rt` that writes only headers.

2) Emit FCM data pushes for TeamCart changes
- Introduce `ITeamCartPushNotifier` (application layer) that gathers member device tokens and calls `IFcmService.SendMulticastDataAsync` with `{ teamCartId, version }`.
- Wire this into existing TeamCart event handlers where the VM is mutated: Created, MemberJoined, ItemAdded/Updated/Removed, Tip/Coupon applied/removed, Locked, MemberCommitted (COD/Online), OnlinePaymentSucceeded/Failed, QuoteUpdated, ReadyToConfirm, Converted, Expired.
- Set Android/iOS collapse/TTL flags analogous to Orders MVP.

3) Contract tests
- Add contract tests for `/rt`: 200 + headers, 304 on `If-None-Match`, 401 without auth, 404 when not visible.

4) Client guidance + docs
- Document the `/rt` ETag contract and examples; cross‑link from Orders tracking doc.
- Provide a short client algorithm snippet for polling + FCM dedupe.
- Add Option A appendix: client diff rules; examples of activity strings.

5) Observability and limits (quick win)
- Log push payloads at debug with cartId/version and token counts; metrics for send success/failure.
- Optional soft rate limit for `/rt` per user/cart once validators are in place.

---

Out of Scope / Later
- `sinceVersion` support to return minimal change summary.
- Typed delta pushes over SignalR.
- Per‑cart FCM topic fan‑out (start with direct tokens; add topics if needed).
- Thin `/status` projection for mobile if payload minimization is needed later.

---

Risks & Mitigations
- VM missing or stale: fall back to 404 (client will retry) or trigger VM rebuild; keep TTL refreshed on read.
- Push duplication/ordering: client dedupes by `version`; use collapse IDs and short TTLs to minimize stale deliveries.
- Version monotonicity: `RedisTeamCartStore` already increments `Version` per mutation; verify all mutation paths go through it.

---

Timeline (1–2 days)
- Day 1: Add ETag/304 (+ optional HEAD) to `/rt` + 2 happy‑path contract tests.
- Day 2: FCM notifier for key handlers (Created/Items/Locked/Payments/Converted/Expired) + tests; docs update. Consider planning thin `/status` projection if needed later.

---

Notes for Review
- Mirrors the Orders plan (status endpoint + FCM trigger + polling) but for TeamCart MVP we reuse `/rt` as the tracking endpoint to ship faster. `Version` remains the validator; `quoteVersion` is included for financial context but not used for validators.

