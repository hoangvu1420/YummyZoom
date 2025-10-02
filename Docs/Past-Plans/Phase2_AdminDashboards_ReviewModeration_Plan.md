# Phase 2 Plan - Admin Dashboards & Review Moderation
- Date: September 19, 2025

## 1. Context & Objectives
- Phase 1 (Owner profile + coupon management) shipped and contract-tested per `Docs/Past-Plans/Phase1_OwnerProfile_CouponManagement_Plan.md`, enabling self-service owner operations.
- The MVP review on September 18, 2025 (`Docs/Future-Plans/MVP_Implementation_Review_2025-09-18.md`) flags admin dashboards and review moderation as the next critical gaps.
- Product scope from `Docs/Architecture/Features-Design.md` requires platform-wide metrics, admin oversight of orders/restaurants, and an administrative moderation workflow for reviews.
- Goal: deliver production-ready APIs, projections, and moderation flows that align with Clean Architecture + CQRS patterns already in the repo, unblock admin UI clients, and close compliance gaps.

## 2. Current Implementation Snapshot
### 2.1 Admin Ops & Dashboards
- Application layer: `src/Application/Admin` only exposes the maintenance `RebuildFullMenuCommand`; no queries or commands surface metrics, account data, or order overviews for admins.
- Web API: no admin-focused endpoint group beyond notifications (`src/Web/Endpoints/Notifications.cs`) and registration approvals (`src/Web/Endpoints/RestaurantRegistrations.cs`). There is no `/api/v1/admin/...` surface for dashboards.
- Read models: `src/Infrastructure/Persistence/ReadModels` covers Full Menu, Search, and Reviews. There are no persisted projections or Dapper queries for platform metrics, revenue, or order oversight.
- Data availability: domain tables (`Orders`, `RestaurantAccounts`, `AccountTransactions`, `Users`) exist via `ApplicationDbContext`. Outbox events already capture order and revenue changes, but none project into admin-friendly summaries.
- Realtime: SignalR hubs exist for restaurant and customer dashboards (`src/Web/Realtime/Hubs/RestaurantOrdersHub.cs`, `src/Web/Realtime/Hubs/CustomerOrdersHub.cs`) but no admin channel.

### 2.2 Review Moderation
- Domain aggregate `src/Domain/ReviewAggregate/Review.cs` supports `MarkAsModerated()`, `Hide()`, and `Show()` with events (`ReviewModerated`, `ReviewHidden`, `ReviewShown`).
- Projections: handlers under `src/Application/Reviews/EventHandlers` recompute summaries and search index entries when moderation visibility changes. The read model (`ReviewSummaryMaintainer`) already filters out hidden reviews.
- Application layer commands exist only for create/delete (`src/Application/Reviews/Commands/CreateReview` and `src/Application/Reviews/Commands/DeleteReview`). No admin-specific commands or queries implement moderation flows.
- Web API review endpoints in `src/Web/Endpoints/Restaurants.cs` support customer submissions and self-deletes, but there is no admin moderation API; admins must not call owner endpoints directly.
- Tests: domain unit tests cover moderation methods (`tests/Domain.UnitTests/ReviewAggregate/ReviewCoreTests.cs`), but functional and contract suites lack admin moderation coverage.

## 3. Desired Phase 2 Outcomes
1. Admin platform dashboards expose aggregated metrics (orders, GMV/revenue, active users/restaurants, coupon usage) with paginated drill-down APIs.
2. Admins can audit and moderate reviews: list reports, mark as moderated, hide/show with reason, view audit history.
3. Authorization, audit trails, and cache invalidation uphold security and data freshness in accordance with existing patterns.
4. Documentation, OpenAPI, and telemetry updates reflect the new admin surfaces for downstream teams.

## 4. Workstreams & Slices
### Workstream A - Platform Metrics & Data Foundations
1. Schema & Projection Setup
   - Add lightweight admin read models under `src/Infrastructure/Persistence/ReadModels/Admin`:
     - `PlatformMetricsSnapshot` (totals for orders, GMV, refunds, active restaurants, active customers, open support tickets, review volume).
     - `DailyPerformanceSeries` (date buckets for orders/revenue to power charts).
     - `RestaurantHealthSummary` (per-restaurant stats: orders last 7/30 days, avg rating, coupon usage, outstanding balance).
   - Back these with SQL views or Dapper queries leveraging `Orders`, `AccountTransactions`, `RestaurantReviewSummaries`, and `Coupons` tables. Ensure indexes support time-window filters.
2. Maintainers & Scheduling
   - Implement projection services (e.g., `IPlatformMetricsMaintainer`) maintained by a periodic hosted service that mirrors the `ReviewSummaryMaintenanceHostedService` pattern. Configure a short, configurable interval (for example, five minutes) so the job alone keeps metrics current without wiring every domain event.
   - After each recompute pass, publish cache invalidation tags (`cache:admin:platform-metrics`) through `ICacheInvalidationPublisher`, and capture in docs that event-driven top-ups are optional later if tighter freshness is required.
3. Validation & Backfill
   - Supply migration/backfill job to seed metrics from historical data.
   - Document recompute tooling (manual command or admin API) in Ops runbooks.

### Workstream B - Admin Dashboard Queries & APIs
1. Application Queries
   - Implement MediatR queries under `src/Application/Admin/Queries`:
     - `GetPlatformMetricsSummaryQuery` returning top-line KPIs.
     - `GetPlatformTrendsQuery` (time-series for revenue/orders).
     - `ListRestaurantsForAdminQuery` with filters (status, health score, verification) per `Docs/Feature-Discover/3-Restaurant.md`.
     - `ListOrdersForAdminQuery` supporting status filters, problem flags, manual override actions.
     - `ListCouponsForAdminQuery` and `GetCouponCampaignStatsQuery` driven by Phase 1 coupon projections.
2. Web Endpoints
   - Create `src/Web/Endpoints/Admin/AdminDashboards.cs` grouping endpoints under `/api/v1/admin/dashboard/...` with `[Authorize(Roles = Roles.Administrator)]`.
   - Apply consistent pagination models and ProblemDetails responses. Add OpenAPI examples.
   - Consider streaming endpoints (Server-Sent Events or SignalR) for live order feed; defer if not MVP-critical but leave extensibility hooks.
3. Integration Points
   - Coordinate with client team on DTO shape (list vs summary). Provide contract tests in `tests/Web.ApiContractTests/Admin` verifying schemas and auth.
   - Ensure queries leverage `CancellationToken` and `IDbConnectionFactory` for performance.

### Workstream C - Review Moderation Workflow
1. Commands & Validators
   - Add `ModerateReviewCommand`, `HideReviewCommand`, `ShowReviewCommand` under `src/Application/Reviews/Commands/Moderation` implementing `IRequest<Result>` with validators enforcing admin role and optional reason field.
   - Extend `IReviewRepository` with helper methods (e.g., `GetWithDetailsAsync`) if needed.
2. Queries
   - Build `ListReviewsForModerationQuery` and `GetReviewAuditTrailQuery` to surface pending and historical moderation data. Include filters: flagged status, rating thresholds, time windows.
3. Web API Surface
   - Expose endpoints under `/api/v1/admin/reviews`: list, get detail, moderate, hide, show.
   - Accept reason/comment payload for moderation actions and return updated state.
4. Integration With Existing Projections
   - Ensure commands call domain methods so existing event handlers (`ReviewModeratedSummaryHandler`, `ReviewHiddenSummaryHandler`, `ReviewShownSummaryHandler`) keep summaries/search in sync.
   - Propagate moderation state into admin queries (e.g., include `IsModerated`, `IsHidden`, timestamps, actor info).
5. Audit & Notifications
   - Emit structured logs and optional events for moderation actions to feed analytics/support tooling.
   - Hook into notification infrastructure to alert restaurant owners when a review is hidden (configurable).

### Workstream D - Cross-Cutting Concerns
1. Authorization & Identity
   - Confirm administrator roles/claims populated via provisioning scripts and update `Docs/Security-Authorization/Auth-Pattern.md` with new policies.
2. Telemetry & Observability
   - Add OpenTelemetry spans/metrics for admin endpoints (latency, cache hits). Ensure dashboards (Application Insights/Grafana) gain new panels.
3. Caching & Performance
   - Use `CachingBehaviour<T>` on expensive queries; define invalidation on write paths (order lifecycle, review moderation, coupon updates).
4. Documentation & API Spec
   - Update `Docs/Architecture/Features-Design.md` and publish API reference examples.
   - Write onboarding notes for admin UI consumers.
5. Operational Playbooks
   - Provide runbooks for rebuilding metrics projections, handling moderation queues, and monitoring data freshness.

## 5. Test & Verification Strategy
- Unit tests: validation and handler logic for new commands/queries (happy path, conflicts, auth failures).
- Functional tests: extend `tests/Application.FunctionalTests` with admin scenarios (metrics query, review moderation lifecycle). Seed data covering orders, refunds, flagged reviews.
- Contract tests: add `tests/Web.ApiContractTests/Admin` verifying OpenAPI paths, response schemas, and auth requirements for every new endpoint.
- Performance regression: benchmark metrics queries against sample datasets (>=30 days). Target <200 ms P95 for summary endpoints.
- Security testing: ensure non-admin roles receive 403. Add coverage for reason logging and PII masking.

## 6. Dependencies, Risks, Mitigations
- Data quality: metrics rely on accurate `AccountTransactions`. Mitigate with reconciliation job comparing aggregates to raw orders before enabling admin dashboards.
- Event ordering: projections triggered by outbox must tolerate eventual consistency; adopt idempotent handlers and provide retries.
- Scalability: daily aggregates may grow; consider partitioned tables or downsampling if volume exceeds expectations.
- Compliance: review moderation actions must be auditable. Store actor, timestamp, and reason in append-only log.

## 7. Milestone Sequencing
1. Stand up admin read models and metrics maintainer (Workstream A).
2. Deliver core admin queries/endpoints for platform overview and restaurant/order listings (Workstream B slices 1-3).
3. Implement review moderation commands and admin review endpoints (Workstream C).
4. Layer cross-cutting instrumentation, documentation, and contract tests (Workstream D plus Section 5).
5. Run end-to-end verification (seed data, smoke tests) and hand off to admin UI team.

## 8. Exit Criteria
- Admin dashboard endpoints deployed behind feature flag, returning data consistent with backfill baseline.
- Admin review moderation workflow (moderate, hide, show) validated via functional and contract tests; owner/customer experiences reflect updated visibility.
- Documentation and runbooks updated; telemetry dashboards configured.
- No open critical bugs; automated CI covers new commands/queries.
