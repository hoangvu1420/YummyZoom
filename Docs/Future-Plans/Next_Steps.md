### Overall next steps (high-level)

- 1) Complete Application layer use-cases per aggregate
- 2) Implement event-driven read models and background processors
- 3) Expose thin Web endpoints with authorization
- 4) Integrate payments, notifications, search, and real-time updates
- 5) Add tests across layers; wire up CI/CD and observability

### Layer-by-layer guide

- Domain (status: finalized)
  - No changes planned unless uncovered during use-case implementation or validations.
  - Ensure domain events enumerated in `Docs/Architecture/Domain_Design.md` are all raised by existing methods and covered by handlers later.

- Application (CQRS)
  - Finish Commands/Queries feature-by-feature using patterns in `Docs/Development-Guidelines/Application_Layer_Guidelines.md`.
  - Prioritize customer path first, then restaurant ops, then admin/support:
    - Users: registration, profile, addresses, payment methods, email change flow (request/confirm), deactivate/delete + deletion saga. See `Docs/Feature-Discover/1-User.md`.
    - RoleAssignments: create/update/remove; queries by user and restaurant.
    - Restaurant: profile update flows already modeled; add commands for verification and accept/decline orders; Dapper queries for public details/search summaries. See `Docs/Aggregate-Documents/3-Restaurant-Aggregate.md`.
    - Menu/Category/Item: CRUD and enable/disable; availability toggle; queries for owner management and for public browsing (optimized). Build “FullMenuView” read model consumers later.
    - CustomizationGroup: CRUD; attach/detach to menu items.
    - Coupon: CRUD; enable/disable; increment usage on order success; queries for validation UI.
    - Order: end-to-end flow per `Docs/Feature-Discover/10-Order.md` (initiate, webhook handling, status transitions, cancel/reject, reorder). Define `OrderFinancialService`, `IPaymentGatewayService`.
    - Review: create with constraints (delivered orders), moderate/hide, reply.
    - TeamCart: lifecycle “Lock, Settle, Convert” per `Docs/Feature-Discover/14-TeamCart.md`; conversion service to `Order`; scheduler to expire.
    - SupportTicket: create, message, status transitions; admin assignment.
  - Cross-cutting:
    - Validation (FluentValidation) on all commands.
    - Authorization attributes and policies on commands/queries.
    - Result pattern end-to-end.
    - Sagas/Process managers:
      - User deletion (GDPR) to anonymize across `Order`, `Review`, `RoleAssignment`.
      - Order payment finalization (webhook idempotency, refund on failures in downstream steps).
      - TeamCart conversion and payments reconciliation.

- Infrastructure
  - Persistence (status: mappings done). Ensure EF configs keep field access for collections as per project convention.
  - Read models and projections from domain events defined in `Docs/Architecture/Domain_Design.md`:
    - RestaurantReviewSummary
    - CouponUsage
    - FullMenuView
    - RestaurantSearchIndex
    - ReviewEligibility (from delivered orders)
  - Background processing:
    - Reliable outbox/event dispatcher.
    - Job scheduler for TeamCart expiration; retries for projections.
  - External integrations:
    - Payments: implement `IPaymentGatewayService` (Stripe first). Add idempotency store for webhooks.
    - Notifications: email (for receipts), push (for status/TeamCart), and real-time channel fan-out.
    - Search: optional first pass with SQL LIKE; later integrate Azure Cognitive Search/Elasticsearch (see `infra/`).
  - Caching:
    - TeamCart real-time view in Redis.
    - Full menu and public restaurant summaries with invalidation on domain events.

- Web (API)
  - Endpoints per command/query with thin controllers/minimal APIs; versioning aligns with `Docs/API-Design/API_Versioning.md`.
  - Authentication/Authorization:
    - Claims-based and policy checks for Restaurant context actions (owner/staff).
    - Scoping by `RestaurantId` for staff endpoints.
  - Real-time:
    - SignalR hubs for restaurant dashboards (incoming orders/status) and TeamCart collaboration updates.
  - Request/response DTOs only; never expose domain types.

- Testing
  - Unit tests: domain and application services (financial calc, sagas).
  - Functional tests: command/handler happy paths and failures (see `tests/Application.FunctionalTests`).
  - Integration tests: Infrastructure (repo mappings, Dapper queries, payment webhook verification).
  - Contract tests for webhooks and public APIs.
  - Seed data for local runs; fixtures for complex aggregates.

- DevOps, Observability, Security
  - CI: build, test, migrations validation.
  - CD: use `infra/` bicep modules for environments; app config and secrets via Key Vault.
  - Telemetry: structured logging, tracing, metrics; dashboards via Application Insights.
  - Security: input validation, rate limiting on public endpoints, webhook signature checks, least-privilege DB accounts.

### Concrete deliverables and order of execution

- Phase 1: Critical path to place and fulfill orders
  - Implement Orders commands/queries and payment gateway abstraction.
  - Webhook handler + idempotency store.
  - Restaurant dashboard SignalR updates for `OrderPaymentSucceeded` → `Accepted/Preparing/...`.
  - Coupon usage incr. + RestaurantAccount revenue handler.

- Phase 2: Menu management and public browse
  - Menu/Category/Item commands; availability toggles.
  - FullMenuView projector and public menu queries.
  - Restaurant public queries and search index stub.

- Phase 3: TeamCart
  - TeamCart commands, Redis view model, lock/settle/convert pipeline.
  - Scheduler to expire carts.
  - Conversion service to `Order`.

- Phase 4: User lifecycle and admin
  - User registration, profile, secure email change, deactivate/delete saga.
  - RoleAssignments.
  - Admin queries for users/orders/support.

- Phase 5: Reviews and Support
  - Review creation gated by delivered orders; moderation.
  - Support tickets with messaging and assignment.

- Definition of Done per feature
  - Commands, queries, validators, DTOs.
  - AuthZ on endpoints.
  - Domain events handled and read models updated.
  - Tests (unit + functional, and integration if infra touched).
  - Observability hooks and docs update.

- Quick wins to start this week
  - Implement `InitiateOrderCommand`, `HandleStripeWebhookCommand`, and restaurant SignalR notifications.
  - Implement `CouponUsage` read model handler and `RestaurantAccount.RecordRevenue` handler.
  - Expose minimal endpoints for order placement and status tracking.

Status update:
- Read core design docs in `Docs/Architecture/Domain_Design.md`, `Docs/Architecture/Database_Schema.md`, and feature discovery for `User`, `Order`, `TeamCart`, plus `Restaurant` aggregate doc to align scope.
- Outlined a phased, testable roadmap focusing first on the customer ordering journey, then menu and TeamCart, then user/admin, then reviews/support.
- Ready to start Phase 1 with incremental edits and brief checkpoints, aligning with your step-by-step preference.

- We will keep EF configs using field access for collections to respect encapsulation.
- I’ll proceed stepwise and review between steps; say the word and I’ll begin with Orders Phase 1 implementation.