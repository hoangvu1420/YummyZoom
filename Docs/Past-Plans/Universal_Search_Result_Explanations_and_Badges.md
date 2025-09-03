## Universal Search — Result Explanations & Badges: Implementation Plan

This plan delivers feature (2) from Universal_Search_Next_Steps: “Result explanations and badges.” It builds on the completed Faceted Filters and the existing Universal Search MVP.

**Goals**
- Add per-result badges that are simple, informative, and deterministic.
- Add a compact “Reason” string explaining why the result is relevant.
- Keep implementation additive, low-risk, and fast.

**Non‑Goals**
- No ranking changes in this iteration (weights remain the same).
- No new promo integration plumbing; “Promo” badges are future‑gated.

---

**Current State Overview**
- Search endpoint and handler
  - `src/Web/Endpoints/Search.cs:13` maps GET `/api/v1/search` to `UniversalSearchQuery` and returns `UniversalSearchResponseDto`.
  - `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:11` defines the query and response DTOs.
  - `SearchResultDto` fields today: `Id, Type, RestaurantId, Name, DescriptionSnippet, Cuisine, Score, DistanceKm` (`src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:39`).
  - Scoring blends text rank + proximity + open‑now flags (`src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:151`).
- Facets are implemented and tested
  - Facets block computed via Dapper multi‑query; cuisine/tags/price bands/open‑now count.
  - Functional tests exist in `tests/Application.FunctionalTests/Features/Search/UniversalSearchTests.cs` that validate facet correctness and filters.
- Read model (SearchIndexItems)
  - Table, indexes, and tsvector maintenance in migration `20250903060356_AddSearchIndexAndFacets.cs`.
  - Model includes fields needed for badges: `IsOpenNow`, `IsAcceptingOrders`, `AvgRating`, `ReviewCount`, `Geo`.
    - EF model: `src/Infrastructure/Data/Models/SearchIndexItem.cs:19` (IsOpenNow), `:20` (IsAcceptingOrders), `:21` (AvgRating), `:22` (ReviewCount), `:25` (Geo).
  - Upsert maintainer populates rating/accepting state from primary tables (`src/Infrastructure/ReadModels/Search/SearchIndexMaintainer.cs:178`). Note: `IsOpenNow` is currently stubbed `false` until business hours logic is wired.
- Tests infra
  - Functional test helpers can directly tweak read model rows for scenario setup (e.g., toggling `IsOpenNow`) using `ApplicationDbContext`.

What’s available to use
- Proximity (`DistanceKm`) is already computed in the search query.
- Flags for open‑now and accepting orders exist in the read model.
- Ratings/ReviewCount are available for creating a rating badge.
- No promo wiring in the read model yet; treat as future/optional.

---

**Feature Definition**
- Badges (examples)
  - `open_now`: When `IsOpenNow` AND `IsAcceptingOrders` are true.
  - `near_you`: When `DistanceKm` is not null and within a configurable threshold (default: ≤ 2.0 km).
  - `rating`: Display “⭐ 4.6 (1.1k)” when `AvgRating` and `ReviewCount` are present; show with 1 decimal; `ReviewCount` abbreviated (e.g., 1,234 → “1.2k”). Only show when `ReviewCount ≥ 10` by default.
  - `promo` (future‑gated): requires read‑model fields or a join — not in scope unless promo data becomes available in `SearchIndexItems`.
- Reason string
  - A short, deterministic combination of top factors in priority order: Open now → Near you → Rating → Generic text match.
  - Examples: “Open now · 1.2 km away · ⭐ 4.6 (120)”; If distance missing: “Open now · ⭐ 4.6 (1.1k)”. If nothing special: “Relevant match”.

Acceptance Criteria
- Each result includes a `Badges[]` array and a `Reason` string.
- Badge logic is deterministic with documented thresholds.
- Performance: No extra DB roundtrips; p95 within existing budget.

---

**API Changes**
- Extend `SearchResultDto` to include badges and reason
  - New shape (high‑level):
    - `Badges: IReadOnlyList<SearchBadgeDto>`
    - `Reason: string?`
  - `SearchBadgeDto` (typed for i18n/UI flexibility): `Code: string` (e.g., `open_now`, `near_you`, `rating`), `Label: string` (pre‑formatted), `Data: object?` (optional structured payload, e.g., `{ rating: 4.6, count: 120 }`).
  - Wire serialization via record types in the application layer; returned as part of `UniversalSearchResponseDto`.
- Backward compatibility: additive response fields; no breaking change to existing consumers.

---

**Implementation Plan**
1) Add DTOs and options
- Add `SearchBadgeDto` in `src/Application/Search/Queries/UniversalSearch/`.
- Add `ResultExplanationOptions` with defaults (thresholds):
  - `NearYouDistanceKm = 2.0`
  - `MinReviewsForRatingBadge = 10`
  - `HighRatingCutoff = 4.5` (for reason phrasing “Highly rated”)
  - Register as `IOptions<ResultExplanationOptions>` with `appsettings` overrides.

2) Extend query row to fetch needed fields
- Update `SearchResultRow` to include `IsOpenNow`, `IsAcceptingOrders`, `AvgRating`, `ReviewCount`.
- Update `selectCols` to select those columns alongside the existing ones.
  - Reference: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:151`.

3) Implement explanation logic (pure function)
- Add a small helper (static class) `SearchResultExplainer` under the same namespace with:
  - `Explain(SearchResultRow row, ResultExplanationOptions opt, double? requestLat, double? requestLon) => (IReadOnlyList<SearchBadgeDto> badges, string reason)`
- Rules:
  - `open_now` badge when `row.IsOpenNow && row.IsAcceptingOrders`.
  - `near_you` badge when `row.DistanceKm is <= opt.NearYouDistanceKm` (format “X.X km away”).
  - `rating` badge when `row.AvgRating.HasValue && row.ReviewCount >= opt.MinReviewsForRatingBadge` (format “⭐ 4.6 (1.1k)”).
  - Build `reason` by ordered join of available factors: Open now → Near you (with distance) → Rating; fallback: “Relevant match”.
  - No promo badge yet (guard behind option flag if needed later).

4) Map badges and reason in handler
- After `QueryPageAsync`, project each row to `SearchResultDto` with computed badges and reason.
- Keep computation in‑process; no extra SQL.
- Reference mapping site: `src/Application/Search/Queries/UniversalSearch/UniversalSearchQueryHandler.cs:185`.

5) Wire configuration
- Add default `ResultExplanationOptions` in `src/Web/Program.cs` (or existing DI config) with `services.Configure<ResultExplanationOptions>(configuration.GetSection("Search:ResultExplanation"))` and sensible defaults.
- Provide sample overrides in `src/Web/appsettings.Development.json`.

6) Telemetry and safety
- Add debug‑level logs for counts of badges emitted per page (disabled by default) to sanity‑check rollout.
- Ensure label strings are bounded (max length) and sanitized.

7) Documentation
- Update `Docs/Future-Plans/Universal_Search_Next_Steps.md` acceptance notes to link this plan file and thresholds.

---

**Testing Plan**

Unit tests (pure logic)
- New test file: `tests/Application.UnitTests/Search/SearchResultExplainerTests.cs`
- Cases:
  - Open now only → `Badges = [open_now]`, `Reason = "Open now"`.
  - Near you only at 0.8 km → `Badges` includes `near_you` with “0.8 km away”, reason includes distance.
  - Rating only (4.6, 120) → `Badges` includes `rating` with “⭐ 4.6 (120)”.
  - Combined: Open + Near + Rating → all three badges; reason ordered “Open now · 1.2 km away · ⭐ 4.6 (1.1k)”.
  - Below thresholds (e.g., 4.1 rating with 5 reviews) → no rating badge.
  - No distance in request → no `near_you` badge even if `Geo` exists.

Functional tests (end‑to‑end handler)
- Extend `tests/Application.FunctionalTests/Features/Search/UniversalSearchTests.cs` or add `ResultExplanationsAndBadgesTests.cs` with scenarios:
  - `OpenNowBadge_IsReturned_WhenFlagsTrue`
    - Arrange: create restaurant, set `IsOpenNow=true`, `IsAcceptingOrders=true` directly on read model; search without lat/lon.
    - Assert: first item `Badges` contains `open_now`; `Reason` starts with “Open now”.
  - `NearYouBadge_IsReturned_WhenWithinThreshold`
    - Arrange: create restaurant with geo near request lat/lon; run search with `lat/lon` inside 2 km.
    - Assert: contains `near_you` with correctly formatted distance; `Reason` includes distance piece.
  - `RatingBadge_IsReturned_WhenAboveThreshold`
    - Arrange: create restaurant; update read model `AvgRating=4.6`, `ReviewCount=120`.
    - Assert: `Badges` contains `rating` with label “⭐ 4.6 (120)” and appears in `Reason`.
  - `NoBadges_ProducesGenericReason`
    - Arrange: minimal data, no lat/lon, default flags.
    - Assert: `Badges` empty; `Reason` equals “Relevant match”.

Contract/API tests (optional, additive)
- If adding Web contract coverage for `/api/v1/search`:
  - Add a minimal contract test ensuring response contains `badges` array and `reason` string.
  - Keep request/response mocking via the mediator as in other contract tests.

Test utilities
- Reuse established helpers: `DrainOutboxAsync`, direct SQL updates to `SearchIndexItems` via `ApplicationDbContext` (pattern used already in facet tests).

---

**Data & Schema Considerations**
- Current iteration requires no schema changes.
- For future promo badges:
  - Option A: Extend read model with `HasActivePromo` and `PromoPercent` (or a preformatted label); fill via read‑side maintainer joins.
  - Option B: Left join to a materialized view of active promotions in the search query. Prefer A for simplicity and performance.

---

**Rollout & Backward Compatibility**
- Additive response fields; no breaking changes expected.
- Guard display thresholds via `ResultExplanationOptions` for quick tuning.
- Enable/disable specific badges behind options if needed during rollout.

---

**Risks & Mitigations**
- Inaccurate `IsOpenNow` until business hours logic is implemented
  - Mitigation: Treat as best‑effort; enable feature toggle to hide open‑now badge in environments without hours.
- Overly long reason strings
  - Mitigation: Limit to top 2–3 parts and cap label lengths.
- Performance regressions
  - Mitigation: Keep logic in memory; no extra SQL; benchmark with existing load tests if needed.

---

**Definition of Done**
- API returns `badges[]` and `reason` per result.
- Logic covered by unit tests; functional tests cover open/near/rating scenarios.
- Options documented with defaults; developers can tune thresholds via configuration.
- No performance regressions in routine runs.

---

**Implementation Checklist (Step‑by‑Step)**
1) Create `SearchBadgeDto` and `ResultExplanationOptions` (with DI wiring).
2) Update `SearchResultRow` and SQL `selectCols` to fetch extra fields.
3) Implement `SearchResultExplainer.Explain(...)`.
4) Update handler mapping to add `Badges` and `Reason` to `SearchResultDto`.
5) Add unit tests for explainer (thresholds, formatting, combinations).
6) Add functional tests validating badges/reason in end‑to‑end search.
7) Add configuration defaults and sample overrides.
8) Update docs and link from `Universal_Search_Next_Steps.md`.

