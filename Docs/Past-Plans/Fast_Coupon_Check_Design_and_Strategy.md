# Fast Coupon Check — Optimal Design and Strategy

This document proposes a pragmatic, high‑leverage design to surface coupon availability and “best deal” suggestions to customers with low latency, while preserving authoritative validation in existing domain services and handlers. It aligns with the project’s outbox/inbox pattern, TeamCart realtime VM, and current repository architecture.

## Objectives

- Return a fast, user‑friendly list of applicable coupons for a given cart (single order or TeamCart), including:
  - Savings amount per coupon, ranked by best deal first
  - Eligibility flags and reasons (e.g., min order gap)
  - Concise display labels and expirations (expires soon)
- Keep authoritative validation and application in existing flows (OrderFinancialService, command handlers).
- Scale set‑wise across many coupons and items; avoid per‑coupon N× cart scans.

## Current Foundations (Recap)

- Coupon aggregate and value objects enforce core invariants (validity window, min order, usage limits, applies‑to scope, value type).
- OrderFinancialService performs canonical discount calculation and validations (percentage/fixed/free‑item, scope‑based eligibility, capping, min order).
- TeamCart: host locks, apply/remove coupon, recompute Quote Lite; conversion finalizes coupon usage.
- Outbox/inbox pattern: domain events are enqueued and consumed idempotently; we now emit CouponUsed via outbox on authoritative increments.
- Redis VM for TeamCart: low-latency collaboration view and quote projection by version.

## Design Overview

We add lightweight read models + a single fast query to compute suggested coupons and expected savings for the current cart snapshot, then expose this via dedicated query endpoints.

### Core Projections (DB or Cache)

1) ActiveCouponsByRestaurant
- Columns: `CouponId, RestaurantId, CodeUpper, Type, PercentageValue, FixedAmount, FreeItemId, Scope, MinOrderAmount, ValidityStart, ValidityEnd, IsEnabled, UsageLimitTotal, UsageLimitPerUser`.
- Filtered to coupons that could be valid now (window/enablement).

2) ItemCouponMatrix
- Columns: `CouponId, RestaurantId, MenuItemId (nullable), MenuCategoryId (nullable), ScopeMarker`.
- Pre-fanned-out mapping: WholeOrder → marker row; SpecificItems → rows for each item; SpecificCategories → rows for each category.

3) CouponUsageView (or compute on the fly)
- Backed by `Coupons.CurrentTotalUsageCount` and `CouponUserUsages (CouponId, UserId, UsageCount)`.
- Optionally materialized projection with `RemainingTotal, RemainingPerUser` for faster filtering.

Update sources via outbox projectors:
- CouponCreated/Enabled/Disabled/Deleted → ActiveCouponsByRestaurant.
- CouponUsed (outbox) → totals and per-user usage counts.
- (If later added) CouponUpdated events → projections refresh.

### Calculation Pipeline (Fast Path)

Input: cart snapshot as list of `{ MenuItemId, MenuCategoryId, Qty, UnitPriceAtOrder }` (unit price includes customizations to match order math).

1) Pre-aggregate cart facts once:
- `cart_subtotal = SUM(UnitPriceAtOrder × Qty)`
- `per_item_subtotal[MenuItemId]` and `per_category_subtotal[MenuCategoryId]`

2) Fetch candidate coupons:
- From ActiveCouponsByRestaurant WHERE `RestaurantId = ?` AND `IsEnabled = true` AND `now() BETWEEN ValidityStart AND ValidityEnd`.
- Join usage to filter `RemainingTotal > 0` and `RemainingPerUser > 0` (or compute remaining on the fly using counts + limits; allow unlimited when null).

3) Compute eligible subtotal per coupon by scope (set‑wise):
- Whole order: `eligible = cart_subtotal`.
- SpecificItems: `eligible = SUM(per_item_subtotal` for items ∈ matrix for that coupon).
- SpecificCategories: `eligible = SUM(per_category_subtotal` for categories ∈ matrix for that coupon).

4) Compute savings per coupon:
- Percentage: `round(eligible × pct/100, 2)`.
- Fixed: `min(value, eligible)`.
- Free item: if the free item exists in cart → unit price (or × free-qty if policy allows multiple); else `0`.
- Enforce `MinOrderAmount`: if `cart_subtotal < MinOrderAmount` → ineligible; attach `minOrderGap`.
- Cap savings to `eligible` and non-negative.

5) Rank and annotate:
- Keep `Applicable = (savings > 0 and meets constraints)`.
- Sort by `savings DESC`, then `ValidityEnd ASC` (expires sooner), then `Scope simplicity (WholeOrder > Category > Item)`.
- Produce fields: `{code, label, savings, expiresOn, meetsMinOrder, minOrderGap, scope, reasonIfIneligible}`.

Implementation options:
- Preferred: set‑based SQL (one round trip) with JSON input for cart lines.
- Alternative: in‑memory computation when coupon count is small; still pull candidates + matrix once, use dictionaries for O(1) lookups.

### Endpoints and Integration

- Single order cart:
  - `POST /api/v1/coupons/fast-check` with body `{ restaurantId, items: [{ menuItemId, menuCategoryId, qty, unitPrice }] }`.
  - Authenticated user; join usage by `UserId`.
  - Returns list `[{ code, label, savings, reasonIfIneligible, minOrderGap, expiresOn, scope }]` and `bestDeal`.

- TeamCart:
  - `GET /api/v1/team-carts/{id}/coupon-candidates`
  - Build cart snapshot from the cart (SQL aggregate or from Redis VM if unit prices are captured there; if not, pull items from SQL).
  - Same output schema; optionally include QuoteVersion so the UI can correlate.

### Redis Caching (Optional, High ROI)

- Cache ActiveCouponsByRestaurant and ItemCouponMatrix per `RestaurantId` in Redis with modest TTL (e.g., 1–5 minutes) and invalidate on outbox projector writes.
- Users’ per‑user usage is volatile; either compute on the fly from `CouponUserUsages` or cache with short TTL keyed by `(userId, restaurantId)`.
- Keep the calculation stateless; only cache inputs to reduce DB reads.

### Consistency & Authority

- Fast‑check is advisory. Final authority remains:
  - OrderFinancialService for exact math and validation.
  - Command handlers for usage limits (Finalization/Reservation).
- If a coupon shows as applicable but fails at apply time due to concurrent depletion, handlers return a precise error; the UI should degrade gracefully.

## Data Model & Projectors

Schema (PostgreSQL shown; adapt to existing EF mapping conventions).

- `ActiveCouponsByRestaurant`
  - PK: `(RestaurantId, CouponId)`
  - Index: `(RestaurantId, IsEnabled, ValidityStart, ValidityEnd)`
  - Columns: `CodeUpper`, `Type`, `PercentageValue`, `FixedAmount`, `FreeItemId`, `Scope`, `MinOrderAmount`, `ValidityStart`, `ValidityEnd`, `IsEnabled`, `UsageLimitTotal`, `UsageLimitPerUser`

- `ItemCouponMatrix`
  - PK: `(CouponId, MenuItemId, MenuCategoryId)` (allow nulls; enforce at application level)
  - Index: `(CouponId, MenuItemId)` and `(CouponId, MenuCategoryId)`

- `CouponUserUsages`
  - Already present; ensure index `(CouponId, UserId)` and a sum/lookup path.

Outbox consumers to maintain projections:
- `CouponCreated/Enabled/Disabled/Deleted` → upsert/soft‑delete rows in ActiveCouponsByRestaurant and refresh matrix rows from AppliesTo.
- `CouponUsed` → increment totals and optionally per‑user usage projections (or recompute from base tables).
- If/when “coupon updated” events exist: refresh both projections.

## Query Implementation

Option A — SQL one‑query pattern (fastest and simplest operationally):
- Accept cart lines as JSON parameter.
- Build `cart_lines`, `per_item`, `per_cat`, `cart_totals` CTEs.
- Select candidates from ActiveCouponsByRestaurant joined to `CouponUserUsages` for the user (or a `CouponUsageView`).
- Join `ItemCouponMatrix` to aggregate eligible subtotal by scope.
- Compute `raw_savings`, clamp, and produce final rows.
- Map to DTO for API response.

Option B — In‑memory hybrid:
- Load candidates + matrix (cached), and compute savings in C# using dictionaries of per-item and per-category subtotals.
- Simpler to implement; adequate if coupon counts per restaurant are small (e.g., < 200) and cart line counts are moderate.

Recommendation: Start with Option B (in‑memory) for faster delivery and less SQL complexity, then move to Option A once traffic/scale grows.

## Integration Details

- Domain math parity: Use the same unit price mapping as `OrderFinancialService.MapToOrderItems(...)` to ensure consistent totals (cart subtotal and eligible base match order math, including customizations).
- Free‑item coupons: support 1 free unit; if future promos require multiple, add a policy parameter and adjust the calculation accordingly.
- Ranking tie‑breakers: consistent ordering by `CodeUpper` for stability.
- Code normalization: always uppercase `CodeUpper` across projections and queries.

## API Contract (Draft)

Request (single order):
```
POST /api/v1/coupons/fast-check
{
  "restaurantId": "...",
  "items": [
    { "menuItemId": "...", "menuCategoryId": "...", "qty": 2, "unitPrice": 15.99 }
  ]
}
```

Response:
```
{
  "bestDeal": { "code": "SAVE15", "label": "15% off", "savings": 7.50, "expiresOn": "2025-11-01" },
  "candidates": [
    { "code": "SAVE15", "label": "15% off", "savings": 7.50, "meetsMinOrder": true, "minOrderGap": 0, "expiresOn": "2025-11-01", "scope": "WholeOrder" },
    { "code": "FREEDRINK", "label": "Free drink", "savings": 0, "meetsMinOrder": false, "minOrderGap": 2.50, "reasonIfIneligible": "MinAmountNotMet", "scope": "SpecificItems" }
  ]
}
```

TeamCart fast‑check can reuse the same response and omit items in the request (cart ID provides the snapshot).

## Phased Rollout

Phase 1 — MVP (in‑memory hybrid)
- Add query endpoint + handler that:
  - Builds cart facts in memory
  - Loads active coupons + matrix + usage counts (short‑TTL cached by restaurant)
  - Computes savings and returns ranked candidates
- Add functional tests for typical carts: verify savings match `OrderFinancialService` results for the same snapshot.

Phase 2 — Projections + Outbox projectors
- Add ActiveCouponsByRestaurant and ItemCouponMatrix tables and projectors off Coupon events.
- Wire Redis cache invalidation on projector updates.
- Add optional CouponUsageView or compute remaining uses efficiently.

Phase 3 — SQL one‑query optimization
- Migrate the handler to run a single SQL query using JSON cart lines.
- Keep in‑memory implementation as fallback.

Phase 4 — TeamCart UX
- Add `/team-carts/{id}/coupon-candidates` endpoint that computes savings from the cart items (SQL load or VM snapshot) and returns best deal; optionally broadcast a lightweight RT hint to members.

## Testing Strategy

- Unit: coupon ranking and savings math parity (percentage, fixed, free‑item) vs `OrderFinancialService` across scope types.
- Functional: endpoint returns correct best deal and candidate reasons; honors min order gaps; remains fast under 95th percentile (set budget, e.g., <50ms for in‑memory; <100ms SQL path).
- Projection tests: outbox events update projections; drain outbox and verify projection rows.

## Observability

- Metrics: number of candidates returned, request latency, cache hit ratio, projection staleness (last updated timestamp), over-limit occurrences at application time.
- Structured logs: restaurantId, userId, candidateCount, bestSavings, path (in‑memory vs SQL), cache used.

## Future Extensions

- Coupon stacking/compatibility rules: extend ranking to run a small knapsack over already‑computed savings with incompatibility constraints.
- Personalized suggestions: consider user history, preferred categories, or A/B testing labels and ranking tie‑breakers.
- Availability by time/day: extend ActiveCoupons with schedule rules to filter earlier.

---

This strategy leverages existing domain math and the outbox/inbox infrastructure to deliver a fast, reliable “best deal” experience. Start with the in‑memory hybrid for speed of implementation, then evolve to the one‑query SQL plan once scale demands it, while keeping coupon application authoritative in existing handlers.

