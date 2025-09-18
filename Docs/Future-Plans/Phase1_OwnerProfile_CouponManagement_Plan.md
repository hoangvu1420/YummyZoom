# Phase 1 Plan — Owner Profile & Coupon Management
- Date: September 18, 2025

**Goals**
- Enable restaurant owners/staff to self-manage profile (name/logo/description, contact, business hours, location/geo, AcceptingOrders).
- Deliver first‑class coupon management (create/update/enable/disable/delete, list/details/stats) for end‑to‑end discount flows.
- Keep read models and search in sync via domain events; require no client‑side cache flushes.

**In Scope**
- New owner profile commands + endpoints, validators, auth (Owner/Staff), tests.
- Coupon CRUD/list/stats endpoints for Owner; Admin can see all.
- Read‑model/search updates and caching headers on affected GETs.
- OpenAPI updates and contract tests.

**Out of Scope**
- Admin dashboards and global metrics (Phase 2).
- Support tickets/refunds (Phase 3).
- Saved payment methods (optional backlog).

**Architecture Changes**
- Application: new commands/queries for Restaurants and Coupons; validators; handlers using repositories + `IUnitOfWork`.
- Web: new/extended endpoint groups under `src/Web/Endpoints/Restaurants.cs` and `src/Web/Endpoints/Coupons.cs`.
- Infra: reuse existing repos; optional new Dapper queries for coupon stats; optional functional unique index for case‑insensitive coupon codes.
- Read models: rely on existing search/review/menus maintainers; trigger via domain events.

**Owner Profile — Commands & Endpoints**
- Commands (new in `src/Application/Restaurants/Commands/*`):
  - `UpdateRestaurantProfileCommand` (name, description, logoUrl, contact): raises `RestaurantProfileUpdated`, `RestaurantLogoChanged`.
  - `UpdateRestaurantBusinessHoursCommand` (hours string + time zone): raises `RestaurantBusinessHoursChanged`.
  - `UpdateRestaurantLocationCommand` (street/city/state/zip/country, lat/lon optional): raises `RestaurantLocationChanged` and `RestaurantGeoCoordinatesChanged` if lat/lon present.
  - `SetRestaurantAcceptingOrdersCommand` (bool): raises `RestaurantAcceptingOrders` (true/false semantics via event payload).
  - All implement `IRestaurantCommand` for resource authorization and use `IRestaurantRepository`.
- Validators: ensure non‑empty name, valid hours format, geo bounds, phone/email format, length limits.
- Web Endpoints (extend `src/Web/Endpoints/Restaurants.cs` protected group):
  - `PUT /api/v1/restaurants/{restaurantId}/profile` → `UpdateRestaurantProfileCommand`.
  - `PUT /api/v1/restaurants/{restaurantId}/business-hours` → `UpdateRestaurantBusinessHoursCommand`.
  - `PUT /api/v1/restaurants/{restaurantId}/location` → `UpdateRestaurantLocationCommand`.
  - `PUT /api/v1/restaurants/{restaurantId}/accepting-orders` → `SetRestaurantAcceptingOrdersCommand`.
  - Auth: Require Owner or Staff; enforce via policies and `IRestaurantCommand`.
- Read Model & Search Effects:
  - SearchIndexMaintainer already handles business hours, accepting orders, geo; ensure outbox/inbox raises handlers (`src/Application/Search/EventHandlers/*`).
  - For menu/full menu caching, return fresh ETag/Last‑Modified on `GET /menus` and `GET /info` when profile fields change.

**Coupon Management — Commands, Queries, Endpoints**
- Commands (new in `src/Application/Coupons/Commands/*`):
  - `CreateCouponCommand` → returns coupon id; normalizes `Code` (upper) on create.
  - `UpdateCouponCommand` → description, value (percentage/fixed/free), min order amount, applies‑to scope.
  - `EnableCouponCommand` / `DisableCouponCommand`.
  - `DeleteCouponCommand` (soft delete).
- Queries (new in `src/Application/Coupons/Queries/*`):
  - `ListCouponsByRestaurantQuery` (filters: active/enabled, date window, paging).
  - `GetCouponDetailsQuery`.
  - `GetCouponStatsQuery` (total usage, per‑user top N, last used at). Dapper against Orders + `CouponUserUsages`.
- Web Endpoints (extend `src/Web/Endpoints/Coupons.cs` or add `RestaurantCoupons.cs`):
  - `POST /api/v1/restaurants/{restaurantId}/coupons` → create.
  - `PUT /api/v1/restaurants/{restaurantId}/coupons/{couponId}` → update.
  - `PUT /api/v1/restaurants/{restaurantId}/coupons/{couponId}/enable|disable`.
  - `DELETE /api/v1/restaurants/{restaurantId}/coupons/{couponId}`.
  - `GET /api/v1/restaurants/{restaurantId}/coupons?pageNumber=&pageSize=&q=&enabled=&from=&to=`.
  - `GET /api/v1/restaurants/{restaurantId}/coupons/{couponId}`.
  - `GET /api/v1/restaurants/{restaurantId}/coupons/{couponId}/stats`.
  - Auth: Owner/Staff for restaurant scope; Admin may access with `/api/v1/admin/coupons` list later.
- Data & Constraints:
  - Keep current unique `(Code, RestaurantId)`; enforce uppercase at write and compare uppercase at read to get case‑insensitive UX.
  - Optional: migration to unique index on `LOWER(Code), RestaurantId` (Postgres) if needed for strict enforcement.

**DTOs (Requests/Responses) — Sketch**
- `UpdateRestaurantProfileRequest { name, description, logoUrl, phone, email }` → 204.
- `UpdateRestaurantBusinessHoursRequest { businessHours, timeZone }` → 204.
- `UpdateRestaurantLocationRequest { street, city, state, zipCode, country, latitude?, longitude? }` → 204.
- `SetAcceptingOrdersRequest { isAccepting }` → 200 with `{ isAccepting }`.
- `CreateCouponRequest { code, description, value: { type, percentageValue?, fixedAmount?{amount,currency}, freeItemId? }, appliesTo: { scope, itemIds?, categoryIds? }, minOrderAmount?{amount,currency}, validityStartDate, validityEndDate, usageLimitPerUser, totalUsageLimit }` → 201 `{ couponId }`.
- `CouponSummaryDto { id, code, description, type, enabled, validityStartDate, validityEndDate, scope }`.
- `CouponStatsDto { totalUsage, uniqueUsers, lastUsedAt }`.

**Authorization & Security**
- Use existing `Policies.MustBeRestaurantOwner`/`Policies.MustBeRestaurantStaff` via `IRestaurantCommand`/`IRestaurantQuery`.
- Validate restaurant ownership on every write path and coupon scope consistency (item/category IDs belong to restaurant).
- Rate limit profile and coupon mutations; log via OTEL.

**Read Models, Caching, Search**
- Search: existing handlers (`RestaurantBusinessHoursChanged*`, `RestaurantGeoCoordinatesChanged*`, `RestaurantAcceptingOrders*`) update `SearchIndexItems` for restaurants and cascade to items.
- Caching headers: bump ETag/Last‑Modified on `GET /restaurants/{id}/info` and `GET /restaurants/{id}/menus` after profile changes (reuse `HttpCaching` helpers).
- No new read model tables required.

**Testing Plan**
- Unit: validators, command handlers (success + auth + invariants), coupon normalization.
- Functional (Dapper/EF): coupon list filters, stats query correctness, code uniqueness, concurrent enable/disable.
- Contract/API tests (`tests/Web.ApiContractTests`): new endpoints success/401/403/404; OpenAPI includes JWT and new schemas; idempotent 201/204 behavior.
- Search propagation (integration): business‑hours/accepting‑orders toggles reflected in `/api/v1/search` facets/flags.

**OpenAPI & Documentation**
- Extend `src/Web/DependencyInjection.cs` OpenAPI document with request/response examples for each new route.
- Tag endpoints “Restaurants • Management” and “Coupons”.

**Observability & Ops**
- Add OTEL spans for profile/coupon writes; include restaurantId, couponId, code.
- Structured logs on coupon enable/disable and AcceptingOrders changes.

**Backward Compatibility**
- No breaking changes to existing endpoints.
- Optional DB index migration is additive.

**Risks & Mitigations**
- Case sensitivity on `Code`: normalize (upper) at write, compare upper at read; add unique lower() index if production needs it.
- Eventual consistency of search/read models: document <2s propagation; return 202 w/refresh hints only if a rebuild is triggered.
- Over‑posting: use explicit request DTOs; forbid nullables where not intended.

**Timeline & Deliverables (2 Weeks)**
- Week 1
  - Day 1–2: Profile commands/validators/handlers; endpoints + contract tests.
  - Day 3–4: Coupon create/update/enable/disable/delete; normalization; tests.
  - Day 5: List/details endpoints; paging/filtering; OpenAPI examples.
- Week 2
  - Day 1–2: Stats query (Dapper) + endpoint; tests.
  - Day 3: Search/read model propagation checks; ETag updates.
  - Day 4: Harden auth/rate‑limit; OTEL spans/logs.
  - Day 5: Docs update; merge readiness checklist.

**Acceptance Criteria**
- Owner can toggle AcceptingOrders; search results reflect within 2s; `/info` shows updated fields with new ETag.
- Coupon CRUD works with validation; duplicate code (case‑insensitive) rejected within same restaurant.
- Fast‑check/apply flows use newly created coupons; usage limits enforced; enable/disable respected.
- All new endpoints documented in OpenAPI and covered by contract tests (200/201/204/400/401/403/404).

**File Map (Planned)**
- `src/Application/Restaurants/Commands/UpdateRestaurantProfile/*`
- `src/Application/Restaurants/Commands/UpdateRestaurantBusinessHours/*`
- `src/Application/Restaurants/Commands/UpdateRestaurantLocation/*`
- `src/Application/Restaurants/Commands/SetRestaurantAcceptingOrders/*`
- `src/Web/Endpoints/Restaurants.cs` (add routes)
- `src/Application/Coupons/Commands/*` and `src/Application/Coupons/Queries/*`
- `src/Web/Endpoints/Coupons.cs` or `src/Web/Endpoints/RestaurantCoupons.cs`
- `tests/Web.ApiContractTests/Restaurants/OwnerProfileContractTests.cs`
- `tests/Web.ApiContractTests/Coupons/CouponManagementContractTests.cs`

**Implementation Checklist (Vertical Slices)**
- [x] 0. Pre‑flight
  - [x] Review `CouponConfiguration` and `CouponRepository` for constraints/queries to avoid duplication.
        Findings:
        - Unique index on `(Code, RestaurantId)` (case-sensitive). With soft deletes, this still blocks reusing a code from a deleted coupon. Keep for MVP; consider partial unique index on `LOWER(Code), RestaurantId WHERE IsDeleted = FALSE` if we later want case-insensitive uniqueness and reuse after delete.
        - Requireds: `Code` (≤50), `Description`, `ValidityStartDate/ValidityEndDate`, `UsageLimitPerUser`; `IsEnabled` default true. Owned types map `Value` (percentage/fixed/free item) and `AppliesTo` (jsonb lists with value comparers). Repo supports active-by-restaurant filtering, atomic usage increments, and emits `CouponUsed` via outbox on finalize.
        - Plan: normalize codes to UPPER on writes; compare UPPER on reads; assert via tests. Optional migration (Step 8) for strict DB enforcement.
  - [x] Verify search handlers exist for business hours/accepting orders/geo updates.
        Verified: `RestaurantBusinessHoursChangedSearchHandler`, `RestaurantAcceptingOrdersSearchHandler`, `RestaurantGeoCoordinatesChangedSearchHandler` update `SearchIndexItems` and cascade via `SearchIndexMaintainer`.
  - [x] CI/contract tests readiness
        Contract tests are in place; we will extend `tests/Web.ApiContractTests` alongside implementation. No new branch required.

- [x] Slice A — AcceptingOrders Toggle (Owner)
  - [x] App: `SetRestaurantAcceptingOrdersCommand` + validator; implements `IRestaurantCommand`.
  - [x] Handler: uses `IRestaurantRepository` + `IUnitOfWork`; raises `RestaurantAcceptingOrders`.
  - [x] Web: `PUT /api/v1/restaurants/{restaurantId}/accepting-orders` → 200 `{ isAccepting }`.
  - [x] Tests: unit/functional tests for handler; API contract tests (200/401); (authorization policy behaviors covered by functional tests pattern).
  - [x] Notes: `/menu` already emits ETag/Last-Modified; `/info` does not currently emit caching headers. We will address `/info` caching in the cross‑cutting caching task (no behavior change needed for this toggle).

- [x] Slice B — Business Hours Update (Owner)
  - [x] App: `UpdateRestaurantBusinessHoursCommand` (+ validator for non-empty/max length).
  - [x] Handler: calls `Restaurant.UpdateBusinessHours` and raises `RestaurantBusinessHoursChanged` via domain event.
  - [x] Web: `PUT /api/v1/restaurants/{restaurantId}/business-hours` → 204.
  - [x] Tests: functional tests for handler; API contract tests (204 with auth, 401 without). Open-now propagation already covered by existing `UniversalSearchOpenNowTests`.
  - [x] Notes: Timezone-aware ‘open now’ remains a future enhancement; current MVP uses UTC-based evaluator in SearchIndexMaintainer.

- [x] Slice C — Location/Geo Update (Owner)
  - [x] App: `UpdateRestaurantLocationCommand` (+ validator: address fields, optional lat/lon bounds).
  - [x] Handler: changes address and, if provided, geo; raises `RestaurantLocationChanged` and `RestaurantGeoCoordinatesChanged`.
  - [x] Web: `PUT /api/v1/restaurants/{restaurantId}/location` → 204.
  - [x] Tests: functional tests (success/address-only, success+geo, not-found, forbidden, validation); API contract tests (204 with auth, 401 without).
  - [x] Notes: `/info` caching headers unchanged (handled later with broader caching story). Search radius behavior will reflect geo via existing search read model handlers.

- [x] Slice D — Profile Basics (name/description/logo/contact) (Owner)
  - [x] App: `UpdateRestaurantProfileCommand` (+ validator for lengths, phone/email).
  - [x] Handler: raises `RestaurantProfileUpdated`, `RestaurantLogoChanged` when applicable.
  - [x] Web: `PUT /api/v1/restaurants/{restaurantId}/profile` → 204.
  - [x] Tests: unit + API contract tests (204/401/403/404).
  - [x] Notes: ETag bump; no search ranking change except text fields for future facets.

- [ ] Slice E — Coupon Create (Owner)
  - [ ] App: `CreateCouponCommand` (+ validator: date window, value, min order, applies‑to; normalize `Code` to UPPER).
  - [ ] Infra: reuse `ICouponRepository.AddAsync`; ensure `GetByCodeAsync` compares UPPER (read side).
  - [ ] Web: `POST /api/v1/restaurants/{restaurantId}/coupons` → 201 `{ couponId }`.
  - [ ] Tests: unit handler; repo read/write for normalization; API contract tests (201/400/401/403/409).
  - [ ] Notes: Case‑insensitive strategy; optional partial unique index on `LOWER(Code), RestaurantId WHERE IsDeleted = FALSE` if stricter DB guarantee required.

- [ ] Slice F — Coupon Update (Owner)
  - [ ] App: `UpdateCouponCommand` (+ validator); normalize `Code` if allowed to change (or keep immutable — decide and document).
  - [ ] Web: `PUT /api/v1/restaurants/{restaurantId}/coupons/{couponId}` → 204.
  - [ ] Tests: unit + API contract tests (204/400/401/403/404/409 on duplicates).
  - [ ] Notes: Validate applies‑to IDs belong to restaurant.

- [ ] Slice G — Coupon Enable/Disable (Owner)
  - [ ] App: `EnableCouponCommand` / `DisableCouponCommand`.
  - [ ] Web: `PUT /api/v1/restaurants/{restaurantId}/coupons/{couponId}/enable|disable` → 204.
  - [ ] Tests: unit + API contract; functional test verifies fast‑check/apply respects enabled flag immediately.

- [ ] Slice H — Coupon Delete (Owner)
  - [ ] App: `DeleteCouponCommand` (soft delete).
  - [ ] Web: `DELETE /api/v1/restaurants/{restaurantId}/coupons/{couponId}` → 204.
  - [ ] Tests: unit + API contract; list should exclude deleted; duplicate code reuse behavior per chosen index strategy.

- [ ] Slice I — Coupon List (Owner)
  - [ ] App: `ListCouponsByRestaurantQuery` (filters: enabled, active window, q; paging).
  - [ ] Web: `GET /api/v1/restaurants/{restaurantId}/coupons` → 200 paged summaries.
  - [ ] Tests: query paging/filtering + API contract tests.

- [ ] Slice J — Coupon Details (Owner)
  - [ ] App: `GetCouponDetailsQuery`.
  - [ ] Web: `GET /api/v1/restaurants/{restaurantId}/coupons/{couponId}` → 200.
  - [ ] Tests: API contract + not found.

- [ ] Slice K — Coupon Stats (Owner)
  - [ ] Infra: Dapper query for totals/unique users/last used (`IDbConnectionFactory`).
  - [ ] App: `GetCouponStatsQuery`.
  - [ ] Web: `GET /api/v1/restaurants/{restaurantId}/coupons/{couponId}/stats` → 200.
  - [ ] Tests: functional correctness of stats; API contract.

- [ ] Cross‑Cutting — Tests, Observability, Docs
  - [ ] Extend Swagger contract test to assert new schemas/paths present.
  - [ ] Add OTEL spans + structured logs for each slice (restaurantId, couponId, code).
  - [ ] Add basic rate limiting for mutation endpoints.
  - [ ] Ensure consistent ProblemDetails on validation/conflict.
  - [ ] Update Web.http examples; add release notes and any migration steps.
