
### Scope
- Admin-only moderation workflow for reviews: list, detail, moderate, hide, show, with reason and audit trail.
- Integrates with existing domain events and projections; uses CQRS, MediatR, Dapper for queries, repositories + aggregates for commands, and `[Authorize(Roles = Roles.Administrator)]`.

### Domain model alignment
- Reuse existing `Review` aggregate methods and events:
  - `MarkAsModerated()` → emits `ReviewModerated`
  - `Hide()` → emits `ReviewHidden`
  - `Show()` → emits `ReviewShown`
- No aggregate changes required unless we want to capture reason/actor in-domain. For now, keep the aggregate pure; store moderation audit in an append-only read model table.

### Application Layer changes
- New commands under `src/Application/Reviews/Commands/Moderation`:
  - `ModerateReviewCommand(Guid ReviewId, string? Reason)` → `Result`
  - `HideReviewCommand(Guid ReviewId, string? Reason)` → `Result`
  - `ShowReviewCommand(Guid ReviewId, string? Reason)` → `Result`
- Command traits:
  - `[Authorize(Roles = Roles.Administrator)]`
  - Validators:
    - `ReviewId` required
    - `Reason` optional but when supplied: trimmed, `<= 500` chars
  - Handler pattern:
    - Resolve `IUser` to capture actor info (DomainUserId or parse `user.Id`)
    - Load via `IReviewRepository.GetByIdAsync`
    - Return NotFound if missing
    - Apply domain operation (`MarkAsModerated` / `Hide` / `Show`)
    - Persist via `IReviewRepository.UpdateAsync`
    - Within `IUnitOfWork.ExecuteInTransactionAsync`
    - Emit audit record using a moderation audit writer (see Read Models)
    - Return `Result.Success()`
- New queries under `src/Application/Reviews/Queries/Moderation` (Dapper-based):
  - `ListReviewsForModerationQuery(...) : Result<PaginatedList<AdminModerationReviewDto>>`
    - Filters: `IsModerated?`, `IsHidden?`, `MinRating?`, `MaxRating?`, `HasTextOnly?`, `FlaggedOnly?` (if flagging exists later), `FromUtc?`, `ToUtc?`, `RestaurantId?`, free-text `Search?` (comment, restaurant name)
    - Sorting: newest, lowest rating, highest rating
  - `GetReviewAuditTrailQuery(Guid ReviewId) : Result<List<ReviewModerationAuditDto>>`
  - Optional: `GetReviewDetailForAdminQuery(Guid ReviewId) : Result<AdminModerationReviewDetailDto>`
- DTOs:
  - `AdminModerationReviewDto`: `ReviewId`, `RestaurantId`, `RestaurantName`, `CustomerId`, `Rating`, `Comment`, `SubmissionTimestamp`, `IsModerated`, `IsHidden`, `LastActionAtUtc`
  - `AdminModerationReviewDetailDto`: above + `Reply`, `OrderId`, `RestaurantSummary` (avg rating, totals) if helpful for context
  - `ReviewModerationAuditDto`: `Action` (Moderate/Hide/Show), `Reason`, `ActorUserId`, `ActorDisplayName?`, `TimestampUtc`
- Caching:
  - Use `CachingBehaviour<T>` for `ListReviewsForModerationQuery` with short TTL (e.g., 30s)
  - Invalidate on moderation commands via cache tags like `cache:admin:review-moderation:list` and `cache:restaurant:{id}:review-summary`
  - Rely on existing review summary maintainers to update restaurant-facing stats; moderation list is an admin read model independent of public summaries.

### Infrastructure Layer (Read Models)
- New Dapper-backed read models under `src/Infrastructure/Persistence/ReadModels/Admin/Reviews`:
  - Table: `ReviewModerationAudits`
    - Columns: `Id (PK)`, `ReviewId (GUID)`, `Action (varchar)`, `Reason (nullable)`, `ActorUserId (GUID)`, `ActorDisplayName (nullable)`, `OccurredAtUtc (timestamp)`
    - Index: `IX_ReviewModerationAudits_ReviewId_OccurredAtUtc`
  - Queries using existing `Reviews` table for listing:
    - Moderate list query joins `Reviews` (+ `Restaurants` for name) and optionally `RestaurantReviewSummaries` for quick context
    - Filters are implemented with SQL predicates and `ILIKE` on Search where applicable
- Writer:
  - `IReviewModerationAuditWriter` with `WriteAsync(ReviewId, Action, Reason, ActorUserId, ActorDisplayName, OccurredAtUtc)`
  - Implementation uses `IDbConnectionFactory` to insert audit rows inside the same transaction/session if possible (or at least same `IUnitOfWork` transaction)
- Migration/backfill:
  - Add a DB migration for `ReviewModerationAudits`
  - No backfill needed initially (starts once feature is live)

### Web Layer
- New endpoints group `src/Web/Endpoints/Admin/AdminReviews.cs`:
  - Base: `/api/v1/admin/reviews`, `RequireAuthorization(Roles = Roles.Administrator)`
  - Endpoints:
    - GET `/` → `ListReviewsForModerationQuery` with filters via `[FromQuery]`
    - GET `/{reviewId:guid}` → `GetReviewDetailForAdminQuery`
    - GET `/{reviewId:guid}/audit` → `GetReviewAuditTrailQuery`
    - POST `/{reviewId:guid}/moderate` → body `{ reason?: string }` → `ModerateReviewCommand`
    - POST `/{reviewId:guid}/hide` → body `{ reason?: string }` → `HideReviewCommand`
    - POST `/{reviewId:guid}/show` → body `{ reason?: string }` → `ShowReviewCommand`
  - Responses:
    - Commands: `204 NoContent` on success, ProblemDetails on errors
    - Lists: `200 Ok` `PaginatedList<AdminModerationReviewDto>`
    - Detail/Audit: `200 Ok` Dto lists
  - OpenAPI
    - Add summaries/descriptions and example payloads/results
    - Use consistent pagination query params `pageNumber`, `pageSize`

### Authorization and security
- Commands: `[Authorize(Roles = Roles.Administrator)]`
- Queries: same role restriction
- Validators enforce `Reason` length and trimming; disallow all-whitespace reasons
- Ensure PII in DTOs is limited; include `CustomerId` as GUID but not customer name/email. If needed, add masking rules in docs.
- Audit trail contains actor IDs and optional display names from `IUser` (if provided); avoid storing email/phone.

### Telemetry and logging
- Add structured logs in command handlers with fields: `reviewId`, `action`, `actorUserId`, `isIdempotentNoop` (true if already hidden/shown/moderated)
- Emit OpenTelemetry spans for queries and commands; tag cache hit/miss for `ListReviewsForModerationQuery`

### Cache invalidation
- On `Moderate/Hide/Show` success:
  - Publish cache invalidation tags:
    - `cache:admin:review-moderation:list`
    - `cache:restaurant:{restaurantId}:review-summary`
    - Optional: `cache:public:restaurant-reviews:{restaurantId}` if exists
- The existing review summary maintainer should already recompute on `ReviewModerated/Hidden/Shown` events; document this dependency.

### Error handling
- NotFound: review missing
- Validation: reason too long
- Authorization: 403 for non-admin
- Idempotency: aggregate returns success if already in desired state; mark `isIdempotentNoop=true` in logs; still write an audit row with action and reason if provided

### Implementation notes
- Use `IUnitOfWork.ExecuteInTransactionAsync` in handlers; perform:
  - Load aggregate → apply domain method → `_reviews.UpdateAsync` → audit writer insert
- Use `IUser` for actor identity:
  - Prefer `DomainUserId`; fallback to parsing `user.Id` as GUID if necessary, otherwise return Unauthorized error
- Validators in the command namespace, one per command, following existing style
- Query performance:
  - Add indexes if missing on `Reviews(IsHidden, IsModerated, SubmissionTimestamp, RestaurantId, Rating)`
  - Use `LIMIT/OFFSET` pagination consistent with `PaginatedList<>`
- Naming and folders:
  - `src/Application/Reviews/Commands/Moderation/{Command}.cs`
  - `src/Application/Reviews/Queries/Moderation/{Query}.cs`
  - `src/Infrastructure/Persistence/ReadModels/Admin/Reviews/*`
  - `src/Web/Endpoints/Admin/AdminReviews.cs`

### Testing
- Unit tests:
  - Command validators: valid/invalid `Reason`, empty/whitespace handling
  - Command handlers: success, NotFound, Unauthorized (no admin), idempotent cases (already hidden/shown/moderated)
- Functional tests `tests/Application.FunctionalTests/Features/Admin/ReviewModeration*`:
  - Seed reviews with varying states; assert command effects and audit rows; check summaries updated via event-driven projection
- Contract tests `tests/Web.ApiContractTests/Admin/AdminReviewModerationContractTests.cs`:
  - Endpoints exist with correct auth
  - Request/response schemas and examples
  - 403 for non-admin roles
- Performance: list query P95 < 200ms on seeded dataset; add indexes if needed

### Example endpoint payloads
- POST `/api/v1/admin/reviews/{reviewId}/hide`
```json
{ "reason": "Contains personal information" }
```
- GET `/api/v1/admin/reviews?pageNumber=1&pageSize=25&isHidden=false&minRating=1&maxRating=2&search=refund`
- GET `/api/v1/admin/reviews/{reviewId}/audit`

### Rollout
- Feature flag the endpoints if desired
- No backfill required; audit starts accruing from first moderation
- Document runbooks for moderation queue handling and cache invalidation

Implementation-ready items:
- Application: 3 commands + 2-3 queries + DTOs + validators
- Infra: `ReviewModerationAudits` table + writer + Dapper SQL for list/detail/audit
- Web: `AdminReviews` endpoints under `/admin/reviews`
- Tests: unit + functional + contract

I’ll proceed with this structure when you’re ready to implement.

- Added a concrete, file-by-file change plan for commands, queries, endpoints, read models, and tests.
- Clarified DTOs, routes, validation, caching, telemetry, and authorization to align with your Clean Architecture and existing patterns.