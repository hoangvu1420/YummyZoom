# Restaurant Aggregated Details Endpoint – Technical Plan
_Prepared on 20 October 2025_

## Executive Summary
- Introduce a new public endpoint `GET /api/v1/restaurants/{id}/details` that bundles public info, full menu, and review summary in a single payload to cut client roundtrips.
- Deliver first-class HTTP caching (ETag + Last-Modified) by aggregating the strongest freshness signals our data already exposes.
- Reuse existing application queries where it keeps maintenance low, but consolidate database access to avoid triple roundtrips.
- Ensure forward compatibility with existing `/info`, `/menu`, and `/reviews/summary` consumers while paving the way for reuse (e.g., allowing `/info` to call into the new aggregation if desired).

## Current State Analysis
- **Public Info (`/info`)**  
  - Handler: `Restaurants.cs:717` orchestrating `GetRestaurantPublicInfoQuery` (Dapper query into `Restaurants`) and `GetRestaurantReviewSummaryQuery`.  
  - Adds rating fields by issuing a second MediatR request (extra DB hit). No HTTP caching or conditional headers. Lat/Lng bypasses cache on purpose.
- **Full Menu (`/menu`)**  
  - Handler: `Restaurants.cs:645` calls `GetFullMenuQuery`. Returns a raw JSON string (pre-assembled `FullMenuViews` row). Implements weak ETag + Last-Modified via `HttpCaching.BuildWeakEtag`.
- **Review Summary (`/reviews/summary`)**  
  - Handler: `Restaurants.cs:693` simply forwards `GetRestaurantReviewSummaryQuery`. No caching headers.
- **Data Sources**  
  - Public info query lacks last-modified metadata (`Restaurants.LastModified`).  
  - Menu view exposes `LastRebuiltAt`.  
  - Review summary exposes `UpdatedAtUtc`.  
  - Each query spins up its own connection via `IDbConnectionFactory`.
- **Testing Coverage**  
  - Contract tests exist per endpoint (e.g., `tests/Web.ApiContractTests/Restaurants/InfoContractTests.cs`).  
  - No combined scenario or caching tests across endpoints.

## Pain Points & Constraints
- Clients must orchestrate three endpoints, increasing latency and risking partial failures.
- Public info endpoint already performs redundant work (fetching review summary again) yet still delivers incomplete caching semantics.
- No single timestamp today represents “restaurant details freshness,” making conditional requests impossible without additional stitching.
- Menu response is text; aggregating it naïvely can force double encoding unless we manage JSON carefully.
- Lat/Lng parameters introduce personalized data; any caching strategy must respect that variance.

## Proposed API Contract
- **Route**: `GET /api/v1/restaurants/{restaurantId}/details`
- **Query Parameters**:  
  - `lat`, `lng` (optional, identical semantics to `/info`; when provided we skip public caching).  
  - Potential `includeMenu=false` toggle left out for now; initial release always returns all three sections.
- **Response Shape (JSON)**:
  ```jsonc
  {
    "info": { /* existing RestaurantPublicInfoResponseDto fields + avgRating + ratingCount */ },
    "menu": { "lastRebuiltAt": "2025-10-19T16:22:44Z", "data": { /* parsed FullMenuViews.MenuJson */ } },
    "reviewSummary": {
      "averageRating": 4.3,
      "totalReviews": 127,
      "ratingsBreakdown": { "1": 2, "2": 4, "3": 11, "4": 39, "5": 71 },
      "totalWithText": 58,
      "lastReviewAtUtc": "2025-09-30T20:15:00Z",
      "updatedAtUtc": "2025-10-18T11:05:17Z"
    }
  }
  ```
- **Conditional Responses**:  
  - `304 Not Modified` when the client’s ETag matches computed freshness and lat/lng omitted.  
  - `Cache-Control: public, max-age=120` (two minutes) aligns with existing info caching.

## Application Layer Design
- **New Query**: `GetRestaurantAggregatedDetailsQuery` in `Application/Restaurants/Queries`.  
  - Shape: returns a DTO containing public info (including rating fields), full menu (as `JsonDocument` or raw string + metadata), review summary, plus freshness metadata.  
  - Implements `ICacheableQuery` when lat/lng not provided (reuse TTL of two minutes with restaurant-scoped key).
- **Data Fetch Strategy**:
  1. Use a single Dapper `QueryMultipleAsync` call that selects:  
     - Restaurant info (existing SQL extended to also project `LastModified`).  
     - `FullMenuViews` row.  
     - `RestaurantReviewSummaries` row.  
     This keeps the DB interaction to one roundtrip, matching the “optimal” requirement.
  2. Reuse helper methods from existing query handlers (JSON parsing, DTO mapping) to avoid drift. Consider extracting shared mapping functions from `GetRestaurantPublicInfoQueryHandler` into an internal static helper to prevent duplication.
- **Freshness Metadata**:
  - Compute `infoLastChanged` using `COALESCE(r."LastModified", r."Created")`.  
  - Use `FullMenuViews.LastRebuiltAt` and `RestaurantReviewSummaries.UpdatedAtUtc`.  
  - Aggregate freshness timestamp: `lastChanged = Max(infoLastChanged, menuLastRebuiltAt, summaryUpdatedAt)`.  
  - Provide fallback to `DateTimeOffset.UtcNow` if any section is missing (e.g., new restaurant lacks summary).
- **Lat/Lng Handling**:
  - When coordinates supplied, still compute distance but mark payload as non-cacheable (no ETag, add `Cache-Control: no-store`).
  - Cache key for application-level caching omits distance (consistent with current behavior).
- **DTO Updates**:
  - Extend `RestaurantPublicInfoDto` (and response DTO) with non-breaking `DateTimeOffset LastModified` to aid downstream uses.  
  - Provide a new `RestaurantAggregatedDetailsDto` containing nested info, menu metadata, review summary, and aggregated freshness.
- **Error Handling**:
  - Align with existing semantics: if base restaurant info missing, return `Error.NotFound` (404).  
  - If menu or summary missing, respond with sensible defaults (empty object / zeroed summary) while still reporting 200.

## Web Layer Changes
- **Endpoint Registration**: Add mapping in `src/Web/Endpoints/Restaurants.cs` within the public group.
- **Conditional Request Flow**:
  - For lat/lng absent:  
    1. Compute ETag using `HttpCaching.BuildWeakEtag(restaurantId, lastChanged)`.  
    2. Short-circuit with 304 if `If-None-Match` or `If-Modified-Since` hits.  
    3. Set `ETag`, `Last-Modified`, `Cache-Control`.
  - For lat/lng present: omit ETag, set `Cache-Control: no-store`.
- **Serialization**:
  - Shape final object via new response DTO (avoid double encoding).  
  - If `FullMenuViews.MenuJson` is large, avoid re-parsing by returning `JsonDocument` via `JsonSerializer.Deserialize<JsonElement>` once.  
  - Keep payload property casing consistent with existing snake-case/camel-case conventions.
- **Backward Compatibility**:
  - Decide whether `/info` should stop invoking `GetRestaurantReviewSummaryQuery` directly once the aggregator exists (can be a follow-up). For now, retain current `/info` behavior to avoid regression risk.

## Testing Strategy
- **Unit Tests**:
  - New query handler tests covering full dataset, missing menu, missing summary, and lat/lng path.  
  - ETag computation tests to ensure max-timestamp logic.
- **Contract Tests** (`tests/Web.ApiContractTests/Restaurants`):
  - Happy path verifying payload shape & nested sections.  
  - 304 scenario with matching ETag.  
  - Lat/lng request ensuring `Cache-Control: no-store` and distance fields present.  
  - Not-found scenario returning problem details.
- **Integration Tests**:
  - Functional tests verifying application caching behavior and concurrency (ensure single DB call using spy connection, if feasible).  
  - Performance smoke test comparing existing three-call client vs new aggregated endpoint latency.

## Rollout & Observability
- Feature flag the new endpoint until clients adopt, or expose immediately with documentation update.  
- Add structured logging for aggregated query duration and response size to verify performance gains.  
- Monitor cache hit rate via API gateway metrics once deployed.

## Open Questions / Follow-Ups
1. Should `/info` be refactored to call the new aggregation (to remove duplicate summary fetch) once stability confirmed?  
  -> Answer: No, keep as is for now to avoid regression risk.
2. Do we expose toggles like `includeMenu=false` for lightweight mobile clients?  
 -> Answer: All filters, toggles, functions including  query parameters from /info, /menu, and /reviews/summary should be supported in the new endpoint in the same way as they are supported in the individual endpoints.
3. Is parsing large menu JSON into a DOM acceptable, or should we return it as raw JSON string within an envelope to minimize allocations?  
  -> Answer: Parsing into a DOM is acceptable.
4. Do we need to propagate menu `ETag` values to CDN edge caches separately?
  -> Answer: No, the aggregated endpoint handles its own caching.

## Implementation Outline
- **API Contract Alignment**
  - Confirm the `GET /api/v1/restaurants/{id}/details` payload mirrors the doc’s sample structure and that query string coverage includes everything from `/info`, `/menu`, and `/reviews/summary` (currently only `lat`/`lng` plus future-safe placeholders).
  - Document new endpoint in `Docs/API-Documentation/API-Reference/Customer/02-Restaurant-Discovery.md`, referencing caching semantics and parameter parity.
- **Application Layer**
  - Introduce `GetRestaurantAggregatedDetailsQuery` + handler co-located with other restaurant queries.
  - Share mapping logic by extracting helper(s) from `GetRestaurantPublicInfoQueryHandler` (to avoid divergence) and modeling DTOs for aggregated response (info, menu metadata, review summary, lastChanged).
  - Extend `RestaurantPublicInfoDto` with `LastModified` and propagate through existing uses.
- **Data Access & Caching**
  - Build a Dapper `QueryMultipleAsync` that retrieves info, menu view, review summary, and associated timestamps in one trip.
  - Implement `ICacheableQuery` policy identical to `/info` (two-minute TTL) and skip caching when `lat`/`lng` present.
  - Aggregate freshness timestamps via `Max(infoLastChanged, menuLastRebuiltAt, summaryUpdatedAt)` with sensible fallbacks.
- **Web Layer Endpoint**
  - Add a public-group minimal API mapping in `Restaurants.cs`; bind optional query params and forward to new query.
  - Apply `HttpCaching.BuildWeakEtag` with aggregated freshness; honor `If-None-Match` and `If-Modified-Since`.
  - Enforce `Cache-Control: public, max-age=120` when cacheable, `Cache-Control: no-store` when coordinates supplied.
  - Serialize menu payload using a single `JsonDocument` parse (acceptable per answer 3), ensuring camelCase output via default serializer options.
- **Backward Compatibility & Reuse**
  - Keep `/info`, `/menu`, `/reviews/summary` endpoints untouched for now (answer 1) but consider optional delegation later.
  - Ensure new DTO additions remain backward compatible with current consumers/tests.
- **Testing & Observability**
  - Add unit, contract, and functional tests reflecting new caching, parameter handling, and error fallbacks.
  - Instrument handler timing/logging to emit metrics for aggregated response size/time.
  - Update monitoring dashboards to track cache hit rate and adoption.

## Step-by-Step Plan
1. **Model Updates**
   - Add `LastModified` to `RestaurantPublicInfoDto` (with default/backward compatibility).
   - Create `RestaurantAggregatedDetailsDto` and supporting nested records (menu metadata, review breakdown mapping).
2. **Shared Mapping Extraction**
   - Factor reusable mapping/parsing helpers from the existing public info handler into an internal static helper (same namespace) and refactor both handlers to call it.
3. **New Query & Handler**
   - Implement `GetRestaurantAggregatedDetailsQuery` (supporting optional `lat`/`lng`, future query params placeholder) and handler using a single `QueryMultipleAsync`.
   - Compose caching policy (skip cache when personalized coordinates supplied).
   - Calculate aggregated freshness timestamp and include it on DTO.
4. **Web Endpoint Wiring**
   - In `Restaurants.cs`, register `MapGet("/{restaurantId:guid}/details", ...)` within the public group.
   - Bind query parameters (lat/lng + reserved dictionary for future toggles) and send the new query via MediatR.
   - Implement conditional responses (ETag/Last-Modified, 304 logic, cache headers) and `Cache-Control` branching per personalization.
5. **Serialization & Response Shaping**
   - Project handler result into response contract, parsing menu JSON into a DOM once (acceptable per answer 3) and preserving numeric/string fidelity.
   - Validate JSON casing and null-handling align with existing API conventions.
6. **Testing**
   - Unit-tests: No need Unit tests.
   - Contract-tests: new endpoint happy path, ETag 304, lat/lng no-store, not-found.
   - Functional-tests: ensure caching policy honored and single DB call executed (use spy connection or logging assertions).
7. **Documentation & Client Guidance**
   - Update `02-Restaurant-Discovery.md` with endpoint details, parameter parity statement (answer 2), and caching behavior.
   - Notify frontend/mobile teams and provide migration guidance (optional include placeholders for toggles).
8. **Observability & Rollout**
   - Add structured logs/metrics for aggregated handler duration and payload size.
   - Non-breaking deployment: launch endpoint without feature flag (per answer 1) or wrap in toggle if release coordination required.
   - Monitor post-deploy metrics, iterate on optional reuse (e.g., letting `/info` call aggregation) once stability confirmed.
