# API Completeness Analysis – Orders, Tracking, and TeamCart (Refined)

Date: 2025-10-23
Author: Client App Team

Purpose
- Re-assess endpoint sufficiency for cart → checkout → order tracking and TeamCart flows, incorporating the Order Tracking & Real-time Updates workflow.
- Identify remaining gaps and propose concrete, testable enhancements for backend–frontend integration.

Sources Reviewed
- Individual Orders: docs/API-Documentation/API-Reference/Customer/03-Individual-Orders.md (2025-09-22)
- TeamCart Workflow: docs/API-Documentation/API-Reference/Customer/Workflows/02-TeamCart-Collaborative-Ordering.md (2025-09-27)
- Order Tracking & Real-time: docs/API-Documentation/API-Reference/Customer/Workflows/03-Order-Tracking-and-Real-time-Updates.md (2025-09-27)

---

## 1) Confirmed Coverage (No Action Needed)

Orders
- Initiate order: POST /api/v1/orders/initiate — returns clientSecret/paymentIntentId for online payments.
- Status polling: GET /api/v1/orders/{orderId}/status — lightweight payload for 10–15s polling.
- Order details: GET /api/v1/orders/{orderId} — full receipt with items, fees, totals (confirmed in tracking workflow).
- History: GET /api/v1/orders/my — paginated summaries.
- Cancel: POST /api/v1/orders/{orderId}/cancel — rules defined.

Order Tracking
- Real-time hub: wss://<host>/hubs/customer-orders with SubscribeToOrder(orderId) and events:
  - ReceiveOrderPlaced, ReceiveOrderPaymentSucceeded, ReceiveOrderPaymentFailed, ReceiveOrderStatusChanged.
- Polling strategy: prefer hub; fallback to status endpoint; stop on terminal states.

TeamCart
- Core flow covered: create → join → items → lock → tip/coupon → member payments (COD/online) → convert to order.
- Real-time: team cart events over SignalR hub /hubs/teamcart with CartUpdated/Locked/PaymentEvent/ReadyToConfirm/Converted/Expired.

Conclusion: The backend supports end-to-end flows for both individual and group orders, including detailed order retrieval and real-time tracking. The client can implement happy paths without additional endpoints.

---

## 2) Remaining Gaps and Clarified Proposals

P1.1 Idempotency for Mutations
- Problem: Mobile retries and double-taps can create duplicate orders/payments/locks.
- Proposal: Support Idempotency-Key header on all POST/DELETE mutating endpoints (orders initiate/cancel; teamcart add/update/remove item, lock, payments, convert). Server stores key→result for TTL (e.g., 24h). Duplicate key returns first result (HTTP 200/201/204 as applicable).
- Client behavior: Generate UUIDv4 per action; reuse on retry; treat 2xx repeat as success.
- Acceptance: Replaying same request with same key is safe and returns identical semantic result.
-> **Backend response:** This feature has been implemented and is now available. Idempotency key support is implemented through the `IIdempotentCommand` interface and `SimpleIdempotencyBehaviour` middleware. Supported endpoints include: `POST /api/v1/orders/initiate`, `POST /api/v1/team-carts`, `POST /api/v1/team-carts/{id}/items`, `POST /api/v1/team-carts/{id}/lock`, and `POST /api/v1/team-carts/{id}/convert`. The system uses Redis caching with 5-minute TTL for successful responses and proper UUID v4 validation. See the [Idempotency section](API-Documentation/03-Core-Concepts.md#idempotency) in the Core Concepts documentation for complete implementation details and usage examples.

P1.2 Pricing Preview (Authoritative)
- Problem: Client mirrors pricing (tax 8%, fee 2.99) which can drift by restaurant/promotion.
- Proposal: POST /api/v1/pricing/preview with { restaurantId, items[], couponCode?, tipAmount? } → returns { subtotal, discount, deliveryFee, tip, tax, total, currency, notes[] }.
- Client behavior: Call before showing final review when online; cache briefly; reconcile deltas in UI.
- Acceptance: Preview totals match order totals at initiate unless state changed (then response includes change notes).
-> **Backend response:** This feature has been implemented and is now available. The pricing preview endpoint provides authoritative, server-side pricing calculations including subtotals, taxes, delivery fees, discounts, and tips. It includes comprehensive validation, customization support, and performance optimization with 2-minute caching. The endpoint uses the same pricing logic as order initiation to ensure consistency. See the [Pricing Preview API documentation](API-Documentation/API-Reference/Customer/05-Pricing-Preview.md) for complete implementation details and usage examples.

P1.3 Coupon Validation (Pre-submit)
- Problem: Validating coupon only on initiate produces poor UX.
- Proposal: POST /api/v1/coupons/validate with { restaurantId, items[], couponCode } → { valid: bool, discount?, message }.
- Client behavior: Validate on entry, show discount preview.
- Acceptance: Invalid codes provide specific messages; server uses same rules as initiate.
-> **Backend response:** This functionality is already available through the existing Fast Coupon Check endpoint. The client can derive single-coupon validation by filtering the fast-check results for a specific coupon code. The existing endpoint provides comprehensive coupon validation, eligibility checks, discount calculations, and detailed error messages using the same authoritative validation logic as order initiation. See the [Fast Coupon Check section](API-Documentation/API-Reference/Customer/03-Individual-Orders.md#fast-coupon-check) in the Individual Orders API documentation for implementation details. No additional development is needed.

P2.1 Payments Status Probe
- Problem: SDK callback interruptions (app killed, deep link lost) leave client uncertain of payment result.
- Proposal: GET /api/v1/payments/{paymentIntentId} → { status: Succeeded|Processing|Failed|Canceled, amount, currency, lastUpdate }.
- Client behavior: On resume, probe and decide whether to poll order status or re-attempt.
- Acceptance: Probe reflects gateway truth within seconds of webhook processing.
-> **Backend response:** This feature is not recommended for MVP. The existing order status tracking infrastructure already provides sufficient payment status information through `GET /api/v1/orders/{orderId}/status` and real-time SignalR events (`ReceiveOrderPaymentSucceeded`/`ReceiveOrderPaymentFailed`). The order status transitions from `AwaitingPayment` to `Placed` (success) or `Cancelled` (failure), which clients can use to determine payment results. Real-time events provide immediate notification of payment outcomes, making direct payment probing unnecessary. This addresses a low-frequency edge case that can be handled through existing endpoints. For MVP, we recommend using order status polling as fallback when real-time events are unavailable.

P2.2 TeamCart Batch Item Operations
- Problem: Promoting personal cart requires N sequential calls (slow, error-prone).
- Proposal: POST /api/v1/team-carts/{id}/items:batch with ops: [ { op: add|update|remove, item: {...} } ].
- Client behavior: Single call to migrate cart; rollback if server returns partial failures.
- Acceptance: Atomic or transactional semantics defined (prefer all-or-none with detailed errors per op on failure).
-> **Backend response:** Promoting personal cart to TeamCart is currently not supported in MVP version. A TeamCart hosted by a customer for a restaurant must be built from the starting point, not by migrating an existing personal cart.

P2.3 TeamCart Membership Management
- Problem: No explicit leave/kick/transfer-host endpoints, complicating moderation.
- Proposal:
  - POST /api/v1/team-carts/{id}/leave (member)
  - POST /api/v1/team-carts/{id}/kick { memberId } (host)
  - POST /api/v1/team-carts/{id}/transfer-host { memberId } (host)
- Client behavior: Present controls to host; update member list via real-time.
- Acceptance: Authorization enforced; events broadcast to members.
-> **Backend response:** Those operations are currently not supported in MVP. Clients should handle membership only via join and cart expiration for now.

P2.4 Share Token Lifecycle
- Problem: No way to revoke/regenerate tokens; security and access control are limited.
- Proposal: POST /api/v1/team-carts/{id}/token { action: regenerate|revoke } and GET for TTL/expiry metadata.
- Client behavior: Host can rotate tokens; joining respects TTL.
- Acceptance: Old tokens invalid after revoke/regenerate; joining with expired token returns 400/401 with code.
-> **Backend response:** TeamCart tokens are currently long-lived and not revocable for MVP. Client should treat them as semi-permanent for now.

P3.1 Capabilities Discovery
- Problem: Feature flag behavior (TeamCart/realtime/payments variants) isn’t discoverable for client gating.
- Proposal: GET /api/v1/capabilities → { teamCartEnabled, realtimeEnabled, ordersHubEnabled, payments: { provider: 'stripe', applePay: bool, googlePay: bool } }.
- Client behavior: Gate UI/flows at startup; adjust polling vs hub.
- Acceptance: Stable across app session; cache with short TTL.
-> **Backend response:** Client does not need to care about feature flags for current MVP version. Just treat all as enabled.

P3.2 Quote Versioning for TeamCart
- Problem: Concurrency—members paying against stale quotes may cause mismatch.
- Proposal: Include quoteVersion (ETag) in lock response and require it in payments/convert; return 409 on mismatch with latest version in body.
- Client behavior: Refresh VM and prompt members to re-confirm.
- Acceptance: No payment accepted against stale quotes; mismatch clearly communicated.
-> **Backend response:** This feature has been implemented and is now available. Quote versioning is included in the lock response (`LockTeamCartForPaymentResponse` with `quoteVersion` field) and optional `quoteVersion` parameters have been added to payment and conversion commands (`InitiateMemberOnlinePaymentCommand`, `CommitToCodPaymentCommand`, `ConvertTeamCartToOrderCommand`). The system validates quote versions and returns 409 Conflict with `TeamCart.QuoteVersionMismatch` error when versions don't match, providing the current version for client retry logic. This prevents payments against stale quotes and ensures financial accuracy in concurrent scenarios. Read the latest documentation in `API-Documentation\API-Reference\Customer\Workflows\02-TeamCart-Collaborative-Ordering.md` for the complete implementation details.

P3.3 Standardized Problem Details `code`
- Problem: Error handling requires parsing free-text `detail`.
- Proposal: Add `code` to all RFC7807 responses (e.g., Coupon.Invalid, TeamCartErrors.InvalidStatusForConversion) while retaining existing fields.
- Client behavior: Switch on `code` for UX decisions; log `type/title/detail`.
- Acceptance: All relevant endpoints return `code`; documented catalog published.
-> **Backend response:** Client should continue to follow the existing Problem Details format for this MVP. Read the `Docs\API-Documentation\03-Core-Concepts.md` for the established error handling approach and conventions.

---

## 3) Optional (Nice-to-Have) Proposals

- Order Reorder Endpoint: POST /api/v1/orders/{orderId}/reorder → returns cart payload to prefill.
- Address Validation: POST /api/v1/addresses/validate → returns normalized address + warnings.
- Deadlines Management: POST /api/v1/team-carts/{id}/deadline { newDeadlineUtc } and optional expire endpoint; enforce policy limits.
- Orders Hub Enhancements: Add ReceiveOrderDeliveredWithRatingPrompt to streamline post-delivery UX.

---

## 4) Backward Compatibility and Rollout

- All new endpoints additive; existing flows remain valid.
- Idempotency-Key support should be no-op for servers that ignore it; clients can start sending immediately.
- Introduce `code` in Problem Details without breaking existing consumers; treat as optional at first.
- Capabilities endpoint enables soft gating of features on older clients.

---

## 5) Acceptance Checklist (for each proposal)

For every shipped proposal above, backend provides:
- OpenAPI update + example payloads
- Error codes with `code` values and HTTP mappings
- AuthZ rules and rate limits
- Postman examples or cURL scripts
- Staging environment ready for client E2E tests

---

## 6) Client Impact Summary

- Reduced duplicate orders via idempotency
- Lower checkout friction via preview and coupon validation
- Faster team cart promotion via batch ops
- Better moderation and security for group orders (membership + token lifecycle)
- More deterministic error handling via standardized codes

