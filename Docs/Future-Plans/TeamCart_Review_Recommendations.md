# TeamCart Feature Review — Prioritized Recommendations

This document reviews the implemented TeamCart components and features against business goals, end‑to‑end flow robustness, and client integration UX. It lists recommendations in descending order of importance.

## Scope Reviewed

- Commands/Queries: `src/Application/TeamCarts/*`
- Real-time VM and store: `src/Application/TeamCarts/Models/TeamCartViewModel.cs`, `src/Infrastructure/StateStores/TeamCartStore/RedisTeamCartStore.cs`
- API endpoints: `src/Web/Endpoints/TeamCarts.cs`
- Realtime: `src/Web/Realtime/Hubs/TeamCartHub.cs`, `src/Web/Realtime/SignalRTeamCartRealtimeNotifier.cs`
- Payments: Stripe integration and webhook handling in `InitiateMemberOnlinePayment`, `HandleTeamCartStripeWebhook`
- Conversion: `ConvertTeamCartToOrderCommandHandler`
- Feature gating: `src/Web/Services/TeamCartFeatureAvailability.cs`
- Tests/docs: `tests/Application.FunctionalTests/Features/TeamCarts/**`, `Docs/Future-Plans/TeamCart_EndToEnd_Integration_Tests.md`

---

## Critical (P0)

1) Webhook failure semantics and member retry UX
- Gap: `src/Application/TeamCarts/Commands/HandleTeamCartStripeWebhook/HandleTeamCartStripeWebhookCommandHandler.cs` treats `payment_intent.payment_failed` as a no‑op and acknowledges success.
- Risk: Clients cannot reflect failed state per member; re‑initiating payments has unclear rules; ReadyToConfirm transition logic might stall/ be ambiguous.
- Recommendation:
  - Add a domain method (e.g., `RecordFailedOnlinePayment(userId, txId, reason?)`) that marks member payment status = Failed and raises `OnlinePaymentFailed`.
  - Update `OnlinePaymentFailedEventHandler` to broadcast and update VM (`PaymentStatus = "Failed"`, clear `OnlineTransactionId`).
  - Expose clear client UX: Failed → member can retry `InitiateMemberOnlinePayment`; guard to prevent multiple concurrent intents per member.
  - Document retry policy and webhook idempotency in client guide.

2) Membership enforcement on realtime hub
- Gap: `src/Web/Realtime/Hubs/TeamCartHub.cs` allows any authenticated user to subscribe; membership enforcement is TODO.
- Risk: Unauthorized users could subscribe and receive updates if they guess `cartId`.
- Recommendation:
  - Require membership check in `SubscribeToCart` using `ITeamCartStore.GetVmAsync` or a lightweight repo check. If not a member, throw `HubException("Forbidden")` and do not join the group.
  - Consider per‑cart authorization token derived from share token for guests.

3) Real‑time financials fidelity and consistency
- Gap: VM totals in `RedisTeamCartStore.RecalculateTotals` compute `Total = Subtotal + Tip - Discount` and initial `DeliveryFee`/`TaxAmount` are zero. During conversion, delivery and tax are added (`ConvertTeamCartToOrderCommandHandler`).
- Risk: Client shows totals that diverge from final order (missing delivery/tax). Poor UX around lock/confirm.
- Recommendation:
  - Provide a “quoted totals” endpoint or enrich VM to include estimated `DeliveryFee` and `TaxAmount` once cart is locked (or earlier if policy allows). Use the same `OrderFinancialService` logic for consistency, or a configured estimator per restaurant.
  - If estimates are not available, surface explicit flags in VM: `TotalsAreEstimated` and exclude delivery/tax from displayed grand total in clients.

4) Idempotency for client‑triggered payment commands
- Gap: Sensitive endpoints (`/payments/online`, `/lock`, `/convert`) do not support client‑supplied idempotency keys.
- Risk: Double‑clicks/retries can lead to duplicate intents or confusing states (mitigated by webhooks, but better to prevent).
- Recommendation:
  - Accept `Idempotency-Key` header and persist minimal request hash + outcome for a bounded TTL. Reuse the existing outbox/inbox patterns to keep handlers idempotent.

---

## High (P1)

5) Join/Share token lifecycle and abuse mitigations
- Status: Join endpoint exists and enforces auth. Share token is masked in VM.
- Recommendation:
  - Rate limit `POST /team-carts/{id}/join` and item‑mutation endpoints.
  - Add share‑token rotation & revocation (host‑only): regenerate token and invalidate old one; optionally track join attempts and lock token after threshold.
  - Add optional token expiry aligned to cart `Deadline/ExpiresAt`.

6) Ready‑to‑Confirm transition clarity
- Status: VM broadcasts for `TeamCartReadyForConfirmation` exist. Business rule is “all members committed or paid”.
- Recommendation:
  - Document exact criteria for ReadyToConfirm (all items mapped to members? handling of zero‑item members? COD commitments completeness?).
  - Include a VM field `ReadyToConfirmAt` and `MissingPayments[]` to help client CTA logic.

7) Redis store contention, atomicity and totals
- Status: Optimistic CAS with `Condition.StringEqual` retries. Works for MVP.
- Recommendation:
  - For high contention, move hot mutations to Lua scripts to reduce read‑modify‑write windows (esp. `AddItem`, `UpdateItemQuantity`).
  - Validate inputs at store boundary (caps on members/items) to guard against oversized payloads.

8) API error contract and codes
- Status: Problems returned via `result.ToIResult()`; codes present in domain errors but not documented centrally.
- Recommendation:
  - Publish a compact error code list and mapping for TeamCart (e.g., `TeamCart.EmptyCart`, `TeamCart.InvalidStatusForConversion`, `TeamCart.CanOnlyPayOnLockedCart`).
  - Include remediation hints for client UX (e.g., “cart must be locked by host”).

9) ETag/versioning for RT GET
- Status: VM has `Version` but `GET /team-carts/{id}/rt` does not emit ETag/If‑None‑Match support.
- Recommendation:
  - Add ETag header based on VM version + cartId. Support 304 responses to reduce bandwidth for polling fallbacks.

10) Observability enhancements
- Status: Good structured logs; outbox metrics present.
- Recommendation:
  - Add counters for: join attempts (success/failed), item mutations, lock attempts (fail reasons), payment intent creations (per member), webhook outcomes (succeeded/failed/ignored), Redis CAS conflicts.
  - Add trace attributes: `teamcart_id`, `member_user_id`, `update_type` for store mutations and SignalR notifications.

---

## Medium (P2)

11) Client integration guide and examples
- Status: End‑to‑end tests cover flows; no dedicated client integration doc.
- Recommendation:
  - Provide a concise guide with code samples:
    - Creating cart, joining, subscribing to SignalR hub (`/hubs/teamcart`), and fetching RT VM.
    - Flow for host: lock → tip/coupon → payments → convert.
    - Member payment UX: initiate → confirm (Stripe) → await webhook status.
    - Fallback when `RealTimeReady` is false: poll `GET /{id}/rt` with ETag.
    - Handling 503 from endpoints (feature off or Redis missing).

12) Validation polish and UX hints
- Status: Validators enforce core rules.
- Recommendation:
  - Extend validation messages to include actionable hints (e.g., which customization group violated min/max and expected ranges) for better client display.

13) Security hardening
- Recommendation:
  - Ensure rate‑limits for mutation endpoints; add antiforgery/CSRF considerations for web clients.
  - Review any PII in realtime messages; keep ShareToken masked in all responses/events.

14) Conversion financial rounding and reconciliation notes
- Status: `ConvertTeamCartToOrder` applies coupon limits and uses `OrderFinancialService`.
- Recommendation:
  - Document how member payments reconcile with final order totals (rounding, proportional allocation, and any deltas). Expose an audit view for operators if needed.

15) Expiration edge cases
- Status: Expiration hosted service registered; webhook policy for missing/invalid metadata marks processed (OK).
- Recommendation:
  - Explicitly document behavior for late webhooks on expired carts and after conversion; continue to record `ProcessedWebhookEvent` to avoid retries, but log with distinct reason.

---

## Nice‑to‑Have (P3)

16) Consistency in route naming and discoverability
- Observation: Plan used `/teamcarts`; implementation uses `/team-carts` (fine). Ensure OpenAPI exposes the full group and tags endpoints coherently.

17) Client‑visible status timeline
- Add optional timestamps (`LockedAt`, `ReadyToConfirmAt`, `ConvertedAt`, `ExpiredAt`) in VM to power a simple status timeline UI.

18) Notifications (push)
- Integrate push notifications (FCM) to notify members on lock/ready/converted, gated by user preferences.

---

## Quick Wins Summary

- Enforce membership in `TeamCartHub` before group join.
- Implement failed payment handling + retry path with event + VM updates.
- Add rate limiting to join and item mutation endpoints.
- Add ETag support for `GET /team-carts/{id}/rt` and document polling fallback.
- Document error codes and ReadyToConfirm criteria for better client UX.

---

## References (file pointers)

- Hub membership: `src/Web/Realtime/Hubs/TeamCartHub.cs:1`
- Webhook handling: `src/Application/TeamCarts/Commands/HandleTeamCartStripeWebhook/HandleTeamCartStripeWebhookCommandHandler.cs:1`
- Conversion: `src/Application/TeamCarts/Commands/ConvertTeamCartToOrder/ConvertTeamCartToOrderCommandHandler.cs:1`
- RT VM: `src/Application/TeamCarts/Models/TeamCartViewModel.cs:1`
- Redis store: `src/Infrastructure/StateStores/TeamCartStore/RedisTeamCartStore.cs:1`
- Endpoints: `src/Web/Endpoints/TeamCarts.cs:1`
- Feature availability: `src/Web/Services/TeamCartFeatureAvailability.cs:1`
- Tests E2E outline: `Docs/Future-Plans/TeamCart_EndToEnd_Integration_Tests.md:1`

