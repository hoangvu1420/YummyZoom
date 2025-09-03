## Restaurant Review Summary Projection: Plan

Purpose: maintain `RestaurantReviewSummaries` (AverageRating, TotalReviews) from review domain events so search results can display reliable rating badges and related explanations.

Scope for this step: review-related events only. Commands and non-review triggers are out of scope here.

Goals
- Deterministic, idempotent updates of `RestaurantReviewSummaries` per restaurant.
- No drift: recompute from source-of-truth (`Reviews`) rather than incremental math.
- Low coupling: a small maintainer service invoked by event handlers.

Non-Goals
- No schema change required (SourceVersion optional; see Idempotency section).
- No direct search-index updates in this step (addressed as a follow-up).

Existing Patterns to Reuse
- Idempotent event handlers: `IdempotentNotificationHandler<T>` with `IUnitOfWork` and `IInboxStore` (e.g., src/Application/Search/EventHandlers/RestaurantCreatedSearchHandler.cs:9).
- Read-model rebuild style using Dapper and `IDbConnectionFactory` (e.g., src/Infrastructure/ReadModels/FullMenu/FullMenuViewRebuilder.cs:15).
- Search upsert pattern for SQL and DI wiring (e.g., src/Infrastructure/ReadModels/Search/SearchIndexMaintainer.cs:7, src/Infrastructure/DependencyInjection.cs:155).

Business Rules (Which reviews count)
- Include: reviews that are visible and not deleted.
  - `Reviews."IsHidden" = FALSE`
  - `Reviews."IsDeleted" = FALSE`
- Moderation gate: configurable. Default include both moderated and unmoderated; allow switching to `IsModerated = TRUE` if product requires.
  - We will express this via an option the maintainer can read; default OFF.
- Rating immutability: domain model does not expose a rating-update command; treat rating as immutable post-create. If this changes later, the recompute still covers it.

Data Model Reference
- `RestaurantReviewSummaries` (PK = `RestaurantId`): src/Infrastructure/Data/Models/RestaurantReviewSummary.cs:3
- `Reviews` table mapping and columns:
  - Table: `"Reviews"`
  - Columns used here: `"RestaurantId"`, `"Rating"`, `"IsHidden"`, `"IsModerated"`, `"IsDeleted"`
  - Mapping: src/Infrastructure/Data/Configurations/ReviewConfiguration.cs:15 (table), :33 (Rating column name = `Rating`), :47 (IsModerated), :49 (IsHidden), plus soft-delete.

Approach
- Create a small maintainer service that recomputes summary metrics from the `Reviews` table for a given restaurant and upserts into `RestaurantReviewSummaries`.
- Wire domain event handlers for review lifecycle events to call the maintainer.
- Keep logic in one SQL statement per event (per-restaurant scope) to minimize roundtrips and avoid race conditions.

Service design
- Interface (Application): `IReviewSummaryMaintainer`
  - `Task RecomputeForRestaurantAsync(Guid restaurantId, long sourceVersion, CancellationToken ct = default)`
- Implementation (Infrastructure): `ReviewSummaryMaintainer` using `IDbConnectionFactory` + Dapper.
- DI registration (Infrastructure): Add `AddScoped<IReviewSummaryMaintainer, ReviewSummaryMaintainer>();` near other read-model services.

SQL (Per-restaurant recompute)
Use a CTE to compute aggregates, then upsert:

```
WITH s AS (
  SELECT
    COALESCE(AVG(r."Rating")::double precision, 0.0) AS avg_rating,
    COALESCE(COUNT(*)::int, 0)                         AS total_reviews
  FROM "Reviews" r
  WHERE r."RestaurantId" = @RestaurantId
    AND r."IsDeleted" = FALSE
    AND r."IsHidden" = FALSE
    -- Optional moderation gate:
    -- AND r."IsModerated" = TRUE
)
INSERT INTO "RestaurantReviewSummaries" ("RestaurantId", "AverageRating", "TotalReviews")
SELECT @RestaurantId, s.avg_rating, s.total_reviews FROM s
ON CONFLICT ("RestaurantId") DO UPDATE
SET "AverageRating" = EXCLUDED."AverageRating",
    "TotalReviews" = EXCLUDED."TotalReviews";
```

Global Rebuild (future/ops)
- For completeness, a full recompute across all restaurants:

```
INSERT INTO "RestaurantReviewSummaries" ("RestaurantId", "AverageRating", "TotalReviews")
SELECT r."RestaurantId",
       AVG(r."Rating")::double precision,
       COUNT(*)::int
FROM "Reviews" r
WHERE r."IsDeleted" = FALSE
  AND r."IsHidden" = FALSE
  -- Optional: AND r."IsModerated" = TRUE
GROUP BY r."RestaurantId"
ON CONFLICT ("RestaurantId") DO UPDATE
SET "AverageRating" = EXCLUDED."AverageRating",
    "TotalReviews" = EXCLUDED."TotalReviews";
```

Event-to-Action Mapping
- `ReviewCreated` (src/Domain/ReviewAggregate/Events/ReviewCreated.cs:7)
  - Action: Recompute for `RestaurantId` from the event payload.
- `ReviewDeleted` (src/Domain/ReviewAggregate/Events/ReviewDeleted.cs:5)
  - Action: Recompute for restaurant of the deleted review. Requires restaurantId lookup; we can either:
    - Query `Reviews` by `ReviewId` to get `RestaurantId` (preferred), or
    - Change event to include `RestaurantId` in future (optional improvement).
- `ReviewHidden` / `ReviewShown` (src/Domain/ReviewAggregate/Events/ReviewHidden.cs:5, ReviewShown.cs:5)
  - Action: Recompute for `RestaurantId` (lookup as above).
- `ReviewModerated` (src/Domain/ReviewAggregate/Events/ReviewModerated.cs:5)
  - If moderation-gated: Recompute; otherwise ignore.
- `ReviewReplied`
  - No change to rating; ignore.

Handlers (Application)
- Namespace: `YummyZoom.Application.Reviews.EventHandlers`
- Pattern: Derive from `IdempotentNotificationHandler<TEvent>` (like search handlers in src/Application/Search/EventHandlers/*).
- Each handler extracts/obtains `RestaurantId`, computes a `sourceVersion` as `e.OccurredOnUtc.Ticks`, and calls:
  - `_reviewSummaryMaintainer.RecomputeForRestaurantAsync(restaurantId, e.OccurredOnUtc.Ticks, ct)`
- Handlers to implement now:
  - `ReviewCreatedSummaryHandler : IdempotentNotificationHandler<ReviewCreated>`
  - `ReviewDeletedSummaryHandler : IdempotentNotificationHandler<ReviewDeleted>`
  - `ReviewHiddenSummaryHandler  : IdempotentNotificationHandler<ReviewHidden>`
  - `ReviewShownSummaryHandler   : IdempotentNotificationHandler<ReviewShown>`
  - `ReviewModeratedSummaryHandler : IdempotentNotificationHandler<ReviewModerated>` (no-op if not gated)

Maintainer (Infrastructure)
- Namespace: `YummyZoom.Infrastructure.ReadModels.Reviews`
- Responsibilities:
  - Implement the per-restaurant recompute SQL shown above.
  - For events without `RestaurantId` (deleted/hidden/shown/moderated), perform a small preliminary query: `SELECT "RestaurantId" FROM "Reviews" WHERE "Id" = @ReviewId`.
  - Make the moderation gate configurable via `IOptions<ReviewSummaryOptions>` (optional; default include unmoderated).

Idempotency & Ordering
- Using recompute-from-source ensures correctness regardless of event ordering or duplicates.
- A `SourceVersion` column in `RestaurantReviewSummaries` is not strictly necessary; the upsert writes a fresh aggregate each time. If desired, we can add `LastRecomputedAt` for diagnostics in a future migration.

Performance & Indexing
- Current indexes (src/Infrastructure/Data/Configurations/ReviewConfiguration.cs:57) include `IX_Reviews_Restaurant_SubmissionTimestamp`. For predicate efficiency, consider a composite index:
  - `CREATE INDEX IF NOT EXISTS IX_Reviews_Restaurant_Visibility ON "Reviews" ("RestaurantId") WHERE ("IsDeleted" = FALSE AND "IsHidden" = FALSE);`
  - If moderation-gated: include `AND "IsModerated" = TRUE` in the partial index.
- Each event reprocesses one restaurant; cost is small even without the new index but advisable at scale.

Testing Plan
- Unit tests (Maintainer): Given seeded `Reviews`, verify upserted `RestaurantReviewSummaries` matches AVG/COUNT with/without hidden/deleted/moderation flags.
- Functional tests (Handlers):
  - On `ReviewCreated`, summary updates for that restaurant.
  - On `ReviewHidden` then `ReviewShown`, counts/average adjust accordingly.
  - On `ReviewDeleted`, counts/average decrease.
- Negative/edge:
  - No reviews: summary row exists with `AverageRating = 0.0`, `TotalReviews = 0`.
  - Mixed moderation when gate ON vs OFF.

Rollout Steps
1) Add `IReviewSummaryMaintainer` interface and `ReviewSummaryMaintainer` implementation.
2) Register maintainer in DI (Infrastructure).
3) Add event handlers for the five review events listed.
4) Add tests (unit + functional) for recompute and handler invocation.
5) (Follow-up) Trigger search index refresh after summary updates to enable rating badges:
   - Either the same handlers also call `ISearchReadModelMaintainer.UpsertRestaurantByIdAsync(restaurantId, sourceVersion)`, or
   - A separate handler/pipeline subscribes to a lightweight in-process notification after recompute.

Open Questions
- Should unmoderated reviews be excluded from summaries? Default here is INCLUDE (more responsive), with a toggle to EXCLUDE if needed.
- Do we want to store a diagnostic timestamp (`LastRecomputedAt`) in `RestaurantReviewSummaries`? Not required for correctness.

Acceptance Criteria
- On any included review event, `RestaurantReviewSummaries` reflects current AVG/COUNT for that restaurant within a single transaction, with no drift.
- Handlers are idempotent via inbox and recompute-from-source logic.
- Performance remains within existing budgets for single-restaurant updates.

