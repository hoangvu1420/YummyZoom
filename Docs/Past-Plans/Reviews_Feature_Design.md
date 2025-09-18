# Reviews Feature – Design (Phase 5.x)

## Overview

Goal: enable customers to write reviews on restaurants, delete their own review, and fetch a restaurant’s reviews alongside an enriched review summary. The design follows established patterns (MediatR commands/queries, AuthorizationBehaviour + PermissionAuthorizationHandler, Dapper read models, outbox-driven projections, caching) and reuses existing components.

## Functional Scope

- Create review
  - Authenticated, fully signed-up users can post a 1–5 star rating with optional title and comment for a restaurant.
  - One active review per user per restaurant (user can delete and re-post; future: edit).

- Delete own review
  - The author can delete their review. Administrators can also delete (for moderation), via existing admin wildcard over user data.

- Get reviews for a restaurant
  - Public endpoint returns paginated list of non-hidden reviews (newest first) and an enriched summary object.

- Enriched review summary
  - Existing fields: average rating, total reviews.
  - Add: count per rating (1–5), last review timestamp, total with text, and optional recent-window counts (e.g., last 30d) to support UI badges.

Non-goals (for later phases): editing reviews; reactions/helpfulness votes; media attachments; owner replies; advanced moderation workflows.

## Authorization

- Foundation: CompletedSignup policy (configured in `Infrastructure/DependencyInjection.cs`) for customer actions.
- Create review
  - `[Authorize(Policy = "CompletedSignup")]`.
  - Model command as restaurant-scoped resource (`IRestaurantCommand`) for consistency, while still relying on CompletedSignup (no special restaurant permission is required to review).

- Delete own review
  - Model as user-scoped resource (`IUserCommand`) with `[Authorize(Policy = Policies.MustBeUserOwner)]` so the owner (or an admin via `UserAdmin:*` claim) can delete.
  - This reuses the existing PermissionAuthorizationHandler logic for owner-or-admin semantics.

- Get reviews & summary
  - Public (no authorization), only returns non-hidden reviews.

## Domain Model

Aggregate: `Review`

- Keys & refs
  - `Id` (Guid), `RestaurantId` (value object), `AuthorUserId` (UserId).
- Data
  - `Rating` (int 1–5), `Title` (<=100, optional), `Comment` (<=1000, optional), `IsHidden` (bool), timestamps (Created/Updated).
- Invariants
  - `1 <= Rating <= 5`.
  - Single active review per `(RestaurantId, AuthorUserId)` enforced via unique index at persistence.
  - Hidden reviews don’t contribute to public queries or summary.
- Events
  - `ReviewCreated`, `ReviewDeleted`, `ReviewHidden` (reuse existing event handler structure in `Application/Reviews/EventHandlers/…`).

Commands on aggregate

- `Create(restaurantId, authorUserId, rating, title?, comment?)` → emits `ReviewCreated`.
- `Delete(byUserId)` → validates author/admin (handled at app layer), emits `ReviewDeleted`.
- `Hide(byUserId, reason)` (admin/moderation) → emits `ReviewHidden`.

## Application Layer

### Commands

- CreateReviewCommand : `IRequest<Result<CreateReviewResponse>>`, implements `IRestaurantCommand`
  - Auth: `[Authorize(Policy = "CompletedSignup")]`.
  - Validations: rating range; lengths; ensure restaurant exists (lightweight check by repository) if needed.
  - Uniqueness: repository throws or domain returns conflict if duplicate exists.
  - Side-effects: write to `Reviews` table; outbox the `ReviewCreated` domain event.

- DeleteReviewCommand : `IRequest<Result>` implements `IUserCommand`
  - Auth: `[Authorize(Policy = Policies.MustBeUserOwner)]`.
  - Behavior: hard-delete row (preferred for summary accuracy/simple UX) or soft-delete + `IsHidden`; either path emits event (`ReviewDeleted` or `ReviewHidden`).
  - Note: choosing hard-delete keeps summary counts consistent with “active reviews.” Hidden remains for moderation path.

### Queries

- GetRestaurantReviewsQuery : returns `PaginatedList<ReviewDto>`
  - Public; filters `IsHidden = false`.
  - Order: newest first; supports page number/size.
  - Uses `IDbConnectionFactory` + Dapper.

- GetRestaurantReviewSummaryQuery : returns `RestaurantReviewSummaryDto`
  - Public; single-row read model fetch.
  - Optionally fetched together with list query to reduce round trips when desired.

### DTOs (suggested)

- `ReviewDto` { ReviewId, AuthorUserId, Rating, Title, Comment, CreatedAtUtc }
- `RestaurantReviewSummaryDto` { AverageRating, TotalReviews, Ratings1, Ratings2, Ratings3, Ratings4, Ratings5, TotalWithText, LastReviewAtUtc }

## Read Models & Projections

Tables (read side)

- `RestaurantReviewSummaries`
  - Existing: `RestaurantId (PK)`, `AverageRating` (double), `TotalReviews` (int).
  - Extend with: `Ratings1 … Ratings5` (int), `TotalWithText` (int), `LastReviewAtUtc` (timestamp), `UpdatedAtUtc`.
  - Backfill/recompute via maintenance job and on-demand recompute.

Projections (outbox-driven)

- Reuse existing handlers:
  - `ReviewCreatedSummaryHandler`, `ReviewDeletedSummaryHandler`, `ReviewHiddenSummaryHandler` in `Application/Reviews/EventHandlers` already call `IReviewSummaryMaintainer`.
  - Update `IReviewSummaryMaintainer.RecomputeForRestaurantAsync` to compute new columns in one SQL statement using CASE/FILTER aggregates.

Maintenance

- Keep `ReviewSummaryMaintenanceHostedService` for periodic recompute/backfill (already present). Update its SQL to include new columns.

Caching

- Use existing `CachingBehaviour<T>` on queries. Invalidate by recompute handlers writing a higher source version (event ticks) into the cache key or using invalidation messages (pattern already used in search read model maintainer).

## Persistence & Indexing

- `Reviews` table (write model)
  - Columns: Id (PK), RestaurantId, AuthorUserId, Rating, Title, Comment, IsHidden, CreatedAtUtc, UpdatedAtUtc.
  - Indexes: `(RestaurantId, CreatedAtUtc DESC)` for listing; `UNIQUE(RestaurantId, AuthorUserId)` for single active review constraint.

- `RestaurantReviewSummaries` table (read model)
  - PK: RestaurantId.
  - Keep schema narrow and computed; no per-user detail here.

## Endpoints (Web)

- `POST /api/v1/restaurants/{restaurantId:guid}/reviews`
  - Body: `{ rating, title?, comment? }` → returns `{ reviewId }`.
  - Auth: CompletedSignup.

- `DELETE /api/v1/restaurants/{restaurantId:guid}/reviews/{reviewId:guid}`
  - Auth: MustBeUserOwner (owner-or-admin via existing claims wildcard).

- `GET /api/v1/restaurants/{restaurantId:guid}/reviews`
  - Query: `pageNumber`, `pageSize` → returns `PaginatedList<ReviewDto>` and optionally embeds `summary`.

- `GET /api/v1/restaurants/{restaurantId:guid}/reviews/summary`
  - Returns `RestaurantReviewSummaryDto`.

All endpoints follow the established `EndpointGroupBase` + `.WithStandardResults()` patterns.

## Errors & Business Rules

- Creation
  - `Reviews.AlreadyExists` for duplicate (same user + restaurant).
  - Validation errors for rating/text limits.

- Deletion
  - `Reviews.NotFound` if review doesn’t exist.
  - `Forbidden` covered by policy if user isn’t owner/admin.

## Search Index Tie-in

- `SearchIndexMaintainer` currently projects `AvgRating` and `ReviewCount`.
- After summary extension, optionally add rating distribution and last review timestamp into the search document to power richer UI facets (non-blocking).

## Testing Strategy

- Application functional tests
  - CreateReview: success, validation failure, duplicate conflict, unauthorized when not signed in.
  - DeleteReview: owner success, not owner forbidden (policy), admin success, not found.
  - List + Summary: pagination order, excludes hidden, summary counts/avg update on create/delete/hide.

- Projection tests
  - Handlers recompute summary on `ReviewCreated/Deleted/Hidden` and update search index row.

## Rollout Plan

1) Migrations for `Reviews` and extended `RestaurantReviewSummaries`.
2) Implement commands/queries, handlers, and endpoints.
3) Update `ReviewSummaryMaintainer` SQL + maintenance hosted service.
4) Add tests and seed data (optional).
5) Ship behind a feature flag if desired (allow read paths first, then writes).

## Open Questions (for product)

- Should reviews require a completed/paid order at the restaurant (verified purchase)? If yes, add a policy or handler rule checking `Order` history.
- Do we keep hard-delete for user deletes, or convert to hide (soft-delete) for audit? Current design prefers hard-delete; moderation uses hide.
- Maximum reviews per time window (spam control)?

This design stays within the current architecture, reuses the authorization and projection infrastructure, and expands the review summary in a single, performant recompute path.

