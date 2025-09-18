# Phase 5 Plan — Restaurant Onboarding (Registration + Admin Approval) and Reviews (MVP)

Version: 0.1 (2025-09-17)
Owner: Core Platform
Status: Planning (targets MVP scope)

## Goals
- Add a restaurant onboarding flow so authenticated users can submit a restaurant registration and administrators can approve or reject it.
- Ship core Reviews capability (create + public listing + ratings summary), gated by delivered orders.
- Defer Support Tickets complexity beyond MVP (keep only placeholders where helpful).

## Context & Fit
- Follows the established Clean Architecture/CQRS pattern per Docs/Development-Guidelines/Application_Layer_Guidelines.md.
- Aligns with MVP capabilities in Docs/Architecture/Features-Design.md (Restaurant management, Admin onboarding/verification, Customer reviews).
- Reuses existing auth model (Roles + policy-based “permission” claims) and Outbox-driven event handlers.
- Current state observations (as of 2025-09-17):
  - Domain has Restaurant aggregate with IsVerified and Verify(); no Application commands yet for create/verify.
  - Web exposes Restaurant-scope management endpoints but not restaurant creation/onboarding routes.
  - Search and public queries do not yet filter by IsVerified (SearchRestaurants); public info returns any non-deleted restaurant.
  - Review domain exists with read-model maintenance handlers; no create/moderation Application commands yet.

## Scope (MVP)
- In-scope now:
  - Restaurant Registration submission (authenticated user) and Admin Approval/Reject.
  - On approval: create Restaurant aggregate (unverified → verify), assign submitter as RestaurantOwner; do not auto-enable accepting orders.
  - Public search should return only verified restaurants; public info allowed only for verified ones.
  - Reviews: create review for delivered orders; list restaurant reviews and my reviews; keep moderation minimal (hide/unhide optional, admin-only).
- Out of scope now (defer):
  - Support Tickets full feature set (keep doc stubs only).
  - Rich admin dashboards or bulk workflows; payments account linking; media uploads pipeline; geo-search beyond basic.

## Key Decisions
- Introduce a lightweight RestaurantRegistration aggregate instead of writing directly to Restaurant:
  - Pros: clear audit trail (submitted/approved/rejected), simpler admin queue, avoids polluting Restaurant with pre-verification fields.
  - Approval composes a Restaurant via domain factory and then calls Verify(); this leverages existing search read-model upserts (RestaurantCreated/RestaurantVerified handlers).
- Authorization:
  - Submit: authenticated user (Roles.User), policy CompletedSignup.
  - Approve/Reject/List pending: Roles.Administrator.
- Visibility rules (public):
  - Search only shows IsDeleted = false AND IsVerified = true.
  - Public info for non-verified returns 404.
- Owner bootstrap:
  - On approval, assign submitter RestaurantRole.Owner via RoleAssignment; this grants RestaurantStaff permissions through existing policy rules. Owners can configure menu before deciding to accept orders.

## Data Model (New)
- Domain aggregate: RestaurantRegistration
  - Fields: Id (Guid), SubmitterUserId (Guid), Name, Description, CuisineType, Address (street, city, state, zip, country), Phone, Email, BusinessHours, LogoUrl?, Latitude?, Longitude?, Status (Pending|Approved|Rejected), SubmittedAtUtc, ReviewedAtUtc?, ReviewedByUserId?, ReviewNote?.
  - Domain events: RegistrationSubmitted, RegistrationApproved, RegistrationRejected (IDs + timestamps + submitter/admin IDs).
- Persistence: EF Core mapping with table RestaurantRegistrations.

## Application Layer (Commands/Queries)
- RestaurantRegistrations
  - Commands
    - SubmitRestaurantRegistrationCommand (auth: CompletedSignup)
      - Creates RestaurantRegistration(Pending) from request + current user id.
      - Validator: all required fields and basic formats; bounds for lat/lng.
      - Response: { RegistrationId }.
    - ApproveRestaurantRegistrationCommand (auth: Administrator)
      - Pre-conditions: status Pending; idempotent (no-op if already Approved/Rejected with clear response).
      - Effect: create Restaurant via Restaurant.Create(...), then estaurant.Verify(); persist; create RoleAssignment(Owner) for submitter; mark registration Approved with reviewer metadata.
      - Response: { RestaurantId }.
    - RejectRestaurantRegistrationCommand(reason) (auth: Administrator)
      - Pre-conditions: status Pending; idempotent (if already not pending, return conflict/ok with current state).
      - Effect: mark registration Rejected with reason.
  - Queries
    - GetMyRestaurantRegistrationsQuery (auth) → list + statuses for current user.
    - GetPendingRestaurantRegistrationsQuery (admin) → paginated list for review.

- Restaurants
  - Commands (small additions for completeness and direct admin/builders)
    - VerifyRestaurantCommand (auth: Administrator) — kept to allow manual toggle later; ApproveRestaurantRegistration already calls verify.
    - SetRestaurantAcceptingOrdersCommand(isAccepting) (auth: MustBeRestaurantOwner/Staff or Administrator) — optional for this phase if not already present.

- Reviews
  - Commands
    - CreateReviewCommand(orderId, restaurantId, rating, comment?) (auth: CompletedSignup)
      - Handler guards: the order belongs to current user and is Delivered; prevent duplicate review per order; use domain Review.Create.
    - Optional admin moderation: HideReviewCommand(reviewId), ShowReviewCommand(reviewId) (auth: Administrator).
  - Queries
    - GetRestaurantReviewsQuery(restaurantId, page) — public; excludes hidden/deleted.
    - GetMyReviewsQuery(page) — auth; reviews by current user.

## Web API (Minimal APIs)
- New endpoint group: RestaurantRegistrations → /api/v1/restaurant-registrations
  - POST / Submit (auth)
  - GET /mine (auth)
  - Admin subgroup (Require Roles.Administrator):
    - GET /pending
    - POST /{registrationId}/approve
    - POST /{registrationId}/reject
- Restaurants (existing group):
  - Optional admin endpoint: POST /api/v1/restaurants/{restaurantId}/verify.
- Reviews
  - POST /api/v1/reviews (auth) — create.
  - GET /api/v1/restaurants/{restaurantId}/reviews (public) — list.
  - GET /api/v1/me/reviews (auth) — my reviews.
  - Optional admin: POST /api/v1/reviews/{reviewId}/hide|show.

## Public Query Adjustments
- Update search to show only verified restaurants:
  - Edit src/Application/Restaurants/Queries/SearchRestaurants/SearchRestaurantsQueryHandler.cs — add ."IsVerified" = true to WHERE.
- Update public info to require verified:
  - Edit src/Application/Restaurants/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQueryHandler.cs — add ."IsVerified" = true to WHERE.

## Infrastructure
- EF Core
  - Add DbSet<RestaurantRegistration> to ApplicationDbContext and fluent config RestaurantRegistrationConfiguration (value objects or flat columns per MVP).
  - Repository: IRestaurantRegistrationRepository with AddAsync, GetByIdAsync, UpdateAsync, ListPendingAsync, ListMineAsync.
- Read Models (Dapper)
  - GetPendingRestaurantRegistrationsQueryHandler & GetMyRestaurantRegistrationsQueryHandler query RestaurantRegistrations.
- Outbox
  - Rely on existing Outbox publisher; registration events are primarily for audit/notifications; approval creates standard Restaurant events that already feed Search index and caches.

## Security & AuthZ
- Commands decorated with [Authorize(...)] attributes per pattern.
- Resource-scoped policies for restaurant endpoints continue to use IRestaurantCommand/Query for permission checks.
- Admin actions rely on Roles.Administrator.

## Notifications (Optional, Nice-to-Have)
- On approval/rejection, send push/email to submitter using IFcmService or email provider stub; can be added via domain event handlers for RegistrationApproved/Rejected.

## Observability
- Log registration submissions and admin decisions with correlation IDs.
- Emit metrics: registrations submitted, approval rate, time-to-approval.

## Step-by-Step Changes (Implementation Plan)
1) Domain
- Add aggregate src/Domain/RestaurantRegistrationAggregate/RestaurantRegistration.cs + Events + Errors + Enums.
- Add EF configuration class src/Infrastructure/Persistence/EfCore/Configurations/RestaurantRegistrationConfiguration.cs.

2) Application — RestaurantRegistrations
- Add repository interface src/Application/Common/Interfaces/IRepositories/IRestaurantRegistrationRepository.cs.
- Add commands + validators + handlers:
  - SubmitRestaurantRegistration
  - ApproveRestaurantRegistration
  - RejectRestaurantRegistration
- Add queries + handlers:
  - GetMyRestaurantRegistrations
  - GetPendingRestaurantRegistrations

3) Application — Restaurants
- Add VerifyRestaurantCommand (admin) if needed for manual toggles post-MVP.

4) Application — Reviews
- Add commands + validators + handlers for CreateReview (and optional HideReview, ShowReview).
- Add queries: GetRestaurantReviews, GetMyReviews.

5) Infrastructure
- Implement RestaurantRegistrationRepository.
- Wire repository & read model services in src/Infrastructure/DependencyInjection.cs.

6) Web
- Add src/Web/Endpoints/RestaurantRegistrations.cs with endpoints as described.
- Add review endpoints in src/Web/Endpoints/Reviews.cs.
- Adjust public queries: update SQL filters in the two handlers listed above.

7) Tests
- Unit: validators; domain creation/approval transitions; duplicate/reject idempotency.
- Functional: submit → approve → verify → role assignment claim appears; submit → reject.
- Contract: API shapes for submissions, pending list, approve/reject; reviews create & list.
- Integration: EF mappings for RestaurantRegistration; Dapper queries for lists; outbox drain for RestaurantCreated/Verified search upserts.

## Rollout & Data
- No breaking changes for existing endpoints.
- Add EF migration for RestaurantRegistrations table.
- Seed (dev): helper endpoint/script to create a few pending registrations for demo.

## Risks & Mitigations
- Duplicate registrations by the same user: enforce validator + unique index on (SubmitterUserId, lower(Name)) per city (optional in MVP; handle at business layer initially).
- Premature owner privileges: owner gets permissions on approval only; before approval they cannot manage any restaurant.
- Search visibility of unverified restaurants: mitigated by WHERE filters.

## Definition of Done (Phase 5 MVP)
- Restaurant registration submission and admin approval/reject work end-to-end with persistence, auth, and basic notifications logging.
- Approved registrations create verified restaurants and assign owner role.
- Public search/info only include verified restaurants.
- Reviews can be created for delivered orders and appear in public listing and summaries.
- Tests cover happy paths + common failure modes; CI green.

## Follow-ups (Post-MVP backlog)
- Support Tickets full feature (Docs/Aggregate-Documents/13-SupportTicket-Aggregate.md) — triage, assignment, messaging, status transitions.
- Rich admin console (filters, bulk actions, triage dashboards).
- Media storage for logos and attachments; moderation tools for reviews.
- Geo-indexed search and facets; open/closed hours awareness for search ranking.
- Webhook/email notifications on registration outcomes.
