# Public Menu Sold Counts Analysis

Status: Draft (October 16, 2025)  
Owners: Web/API Platform  
Related: Docs/Architecture/YummyZoom_Project_Documentation.md, Docs/Architecture/Database_Schema.md, Docs/Development-Guidelines/Read_Models_Guide.md

## 1) Background & Goal
- Frontend wants the public menu endpoint to expose how many units each menu item has sold so customers can see popularity cues (“1.2k sold”, “Best seller”, etc.).  
- The endpoint today streams a pre-built JSON read model (`FullMenuViews`) as-is. Adding dynamic counters needs to preserve fast responses, CDN friendliness (ETag/Last-Modified), and existing API consumers.  
- This analysis surveys the current implementation, identifies relevant data sources, and compares approaches for surfacing rolling “sold” metrics while respecting our read model strategy.

## 2) Current Implementation Snapshot
- **Endpoint contract:** `GET /api/v1/restaurants/{restaurantId}/menu` (public, no auth) returns the raw JSON stored in `FullMenuViews.MenuJson`. It sets weak ETags and `Last-Modified` based on the read model’s `LastRebuiltAt`, then returns `Results.Text` without parsing (`src/Web/Endpoints/Restaurants.cs:640`).  
- **Query handler:** `GetFullMenuQueryHandler` fetches a single row from `FullMenuViews` via Dapper and propagates `MenuJson` plus `LastRebuiltAt` (`src/Application/Restaurants/Queries/GetFullMenu/GetFullMenuQueryHandler.cs:17`). The `GetFullMenuQuery` enforces a 5-minute cache policy and tags invalidation on `restaurant:{id}:menu` (`src/Application/Restaurants/Queries/GetFullMenu/GetFullMenuQuery.cs:7`).  
- **Read model shape:** `FullMenuViews` stores a denormalized document with `version`, `categories`, `items.byId`, `customizationGroups`, and a `tagLegend`. Existing records do not contain any sales statistics (`db_scripts/data.sql`, `db_scripts/schema.sql:629`).  
- **Rebuild pipeline:** `FullMenuViewMaintainer.RebuildAsync` rehydrates menu, categories, items, customization groups, and tags, then serializes JSON and sets `lastRebuiltAt = DateTimeOffset.UtcNow` (`src/Infrastructure/Persistence/ReadModels/FullMenu/FullMenuViewMaintainer.cs:24`).  
- **Freshness management:** Menu changes trigger rebuilds through a wide set of domain event handlers (`MenuItemProjectorBase`, etc.). In addition, `FullMenuViewMaintenanceHostedService` regularly backfills and reconciles the read model, rebuilding restaurants in order of oldest `LastRebuiltAt` (`src/Infrastructure/Persistence/ReadModels/FullMenu/FullMenuViewMaintenanceHostedService.cs:32`). The recon batch defaults to 200 restaurants every 5 minutes (config in `src/Web/appsettings.Development.json`).

## 3) Requirement & Constraints
- **Metric definition (pending clarification):** Frontend used the term “sold counts” without a time window. Candidate semantics include lifetime delivered quantity, rolling 30-day quantity, or a combination (lifetime + rolling window). We should confirm scope before implementation.  
- **Data privacy:** These counts will be public. Stakeholders need to confirm restaurants are comfortable with exposing sales volumes.  
- **Performance:** The menu endpoint must stay cache-friendly and avoid hot-path joins over large tables per request. Rolling up sales inside the read model is attractive, but only if we can keep it current without excessive rebuilds.  
- **Consistency:** ETags/Last-Modified must change when the sold counts change, otherwise clients will cache stale numbers.  
- **Scalability:** The system handles high-order volumes; any approach triggered per order must avoid serial bottlenecks.

## 4) Data Sources & Existing Signals
- **OrderItems snapshot:** `OrderItems` captures `Snapshot_MenuItemId`, `Quantity`, and monetary totals (`Docs/Architecture/Database_Schema.md` around the `OrderItems` table). This is the authoritative source for “how many units of item X were sold”, keyed to menu item IDs.  
- **Order status lifecycle:** We should probably count only orders that reached `Delivered` (or at least exclude `Cancelled` / `Rejected`). Status values live in `OrderStatus` (`src/Domain/OrderAggregate/Enums/OrderStatus.cs`).  
- **Recent popularity logic:** The `GetMenuItemsFeedQueryHandler` already computes a rolling 30-day popularity metric by aggregating `OrderItems` joined to `Orders` (`src/Application/MenuItems/Queries/Feed/GetMenuItemsFeedQueryHandler.cs:45`). We can reuse the SQL patterns and indexes from that query.  
- **Read model cadence:** Menu read models rebuild automatically on menu events but not on order lifecycle events. The reconciliation job processes 200 restaurants per 5-minute pass; for thousands of restaurants, a specific restaurant may wait hours for a rebuild unless a menu change occurs.

## 5) Implementation Options

### Option A — Extend FullMenuView rebuild with sales aggregates
**Idea:** During `RebuildAsync`, run an additional query that aggregates sold quantities for all menu items in the restaurant (e.g., lifetime and rolling 30 days), then embed those numbers into the JSON (new `items.byId[*].sold` object).  

**Key steps**
1. Collect the list of active menu item IDs from the existing rebuild.  
2. Execute a single SQL query such as:
   ```sql
   SELECT
     oi."Snapshot_MenuItemId" AS "ItemId",
     SUM(CASE WHEN o."Status" = 'Delivered' THEN oi."Quantity" ELSE 0 END) AS lifetime_qty,
     SUM(CASE WHEN o."Status" = 'Delivered' AND o."PlacementTimestamp" >= now() - interval '30 days'
         THEN oi."Quantity" ELSE 0 END) AS rolling30_qty,
     MAX(o."UpdatedTimestamp") AS max_status_timestamp
   FROM "OrderItems" oi
   JOIN "Orders" o ON o."Id" = oi."OrderId"
   WHERE oi."Snapshot_MenuItemId" = ANY(@ItemIds::uuid[])
     AND o."Status" = ANY(@CountedStatuses)
   GROUP BY oi."Snapshot_MenuItemId";
   ```
3. Push the aggregated values into the serialized `MenuJson` (e.g., `sold = { lifetime: 1234, rolling30: 87, lastSaleAt: ... }`).  
4. Update DTO version at least logically (`version = 2`) so clients can feature-detect.  
5. Backfill: rerun the maintenance job to rebuild every restaurant once so `sold` appears everywhere.

**Pros**
- Aligns with existing read model pattern: the API continues to serve a single pre-built JSON blob, keeping the handler simple.  
- Low request latency; no additional joins at request time.  
- ETag/Last-Modified remain tied to read model writes.

**Cons / Gaps**
- **Freshness:** Counts only change when the read model rebuilds. With current job settings, restaurants without menu changes might see stale counts for hours.  
- **High-frequency updates:** Rebuilding the full menu JSON on every delivered order is heavy (multiple SQL trips and JSON serialization).  
- **Race conditions:** If we rely solely on the periodic recon job, counts could lag behind reality while ETags remain unchanged, leading clients to cache stale numbers.

**Mitigations**
- Introduce a lightweight queue or flag when an order transitions to `Delivered`, then let the maintenance hosted service prioritize those restaurants (e.g., a new `FullMenuViewPending` table or in-memory channel).  
- Alternatively, allow small JSON patches that increment a single item in-place instead of full rebuild (higher complexity; see Option C).

### Option B — On-demand overlay in the query handler
**Idea:** Keep `FullMenuViews` structural. When `GetFullMenuQueryHandler` returns the JSON, run a second aggregation query for sold counts, merge the results into the JSON document, and return the mutated string. ETag/Last-Modified would incorporate the latest “sold” timestamp.

**Key steps**
1. Fetch the read model row as today.  
2. Aggregate sold counts on the fly (similar SQL as above).  
3. Deserialize the stored JSON into `JsonDocument` or `JsonNode`, merge counts, reserialize.  
4. Compute an effective `LastModified` as `max(readModel.LastRebuiltAt, latestOrderTimestamp)` so caches invalidate when sales change.  
5. Consider adjusting the cache policy to a shorter TTL (e.g., 1 minute) if we cannot easily incorporate invalidation.

**Pros**
- Always reflects the latest order data (within transaction delay).  
- No need to rebuild read model or modify maintenance pipeline.  
- Avoids storing transient metrics in `FullMenuViews`.

**Cons**
- Parsing and reserializing the entire menu JSON for every request adds CPU time and allocations; with ~200 items it is acceptable but worse than streaming the existing string.  
- Harder to maintain backward compatibility: we must ensure the serialized structure stays identical aside from the new field to avoid breaking cached responses.  
- Cache invalidation becomes trickier. Query-level caching currently keyes on `LastRebuiltAt`. If sales change but the read model’s timestamp does not, we must recompute the ETag ourselves and ensure downstream caches respect it.  
- The `ICacheableQuery` TTL (5 minutes) would serve stale counts unless we reduce TTL or integrate dynamic invalidation tags tied to order events.

### Option C — Dedicated `MenuItemSalesSummary` read model
**Idea:** Introduce a new read model table keyed by `(RestaurantId, MenuItemId)` that tracks lifetime and rolling-window counts (and last sale timestamp). Update it incrementally whenever orders transition to counted statuses. The menu endpoint can then join or merge this read model with the stored menu JSON.

**Key steps**
1. Schema: `MenuItemSalesSummaries(RestaurantId, MenuItemId, LifetimeSold, Rolling30Sold, Rolling7Sold, LastSoldAt, LastComputedAt, SourceVersion)`.  
2. Event pipeline: add an async handler for `OrderDelivered` (or similar) that:
   - Unpacks order items.  
   - Batches quantity deltas per `(RestaurantId, MenuItemId)`.  
   - Calls `IMenuItemSalesSummaryMaintainer.UpsertAsync`, incrementing counters idempotently (dedupe by order ID).  
3. Maintenance job: optional batched recompute to correct drift (e.g., nightly job recomputes rolling windows).  
4. Endpoint integration: either (a) extend `FullMenuViewMaintainer` to pull the summary table during rebuild (keeps Option A’s runtime shape but with cheap lookup), or (b) have the query handler overlay counts by fetching the summary table (similar to Option B but using a pre-aggregated source).  

**Pros**
- Keeps expensive aggregation off the hot path; order-driven increments are small (`UPDATE ... SET Rolling30 = Rolling30 + x`).  
- Supports multiple consumers (menu endpoint, analytics dashboards, recommendation services).  
- Rolling windows can be recomputed efficiently with scheduled jobs.  
- Enables accurate ETag updates: summary table can expose `LastUpdatedAt` per restaurant.

**Cons**
- Highest initial implementation cost: new table, maintainer abstraction, event handlers, migrations, tests, ops.  
- Requires careful idempotency (per order per item) to avoid double counting.  
- Rolling windows need sliding-window logic (e.g., store daily buckets or recompute nightly).  
- Slightly increases write load on each delivered order.

### Hybrid / Variant Notes
- **JSON patch approach:** Instead of full rebuilds, create a maintainer method that runs `UPDATE ... SET MenuJson = jsonb_set(...)` to bump counts in place when orders close. This is a hybrid between Options A and C. It keeps the single read model but allows incremental updates. However, manipulating nested JSON with SQL can become brittle and hard to test; also, we must maintain rolling-window math ourselves.  
- **Cache strategy tweaks:** Regardless of option, we may need to adjust `GetFullMenuQuery.Policy` (shorter TTL, additional cache tags) so memoized responses expire when sales data changes.

## 6) Recommended Direction
1. **Confirm the product definition** for “sold counts” (time window, rounding, display rules).  
2. **Implement Option C (new sales summary read model) with Option A-style integration**:
   - Build `MenuItemSalesSummaries` to maintain lifetime + rolling window counts incrementally using `OrderDelivered` events.  
   - Expose maintainer methods to fetch summary snapshots for a restaurant in a single query (including `LastUpdatedAt`).  
   - Update `FullMenuViewMaintainer.RebuildAsync` to look up the summary table and decorate each item with `{ sold: { lifetime, rolling30, lastSoldAt } }`.  
   - Extend `FullMenuViewMaintenanceHostedService` so recon passes also refresh counts (in case of backlog or manual corrections).  
   - Adjust `GetFullMenuQuery` to compute `LastRebuiltAt = max(menu.LastRebuiltAt, summary.LastUpdatedAt)` before setting headers, ensuring caches invalidate promptly.  
   - Backfill the read model using a batched SQL job that sweeps historical `OrderItems`.  
   - Document `version = 2` of the menu JSON and communicate to frontend.

This path balances request-time performance with accurate, timely counts. The incremental table keeps rebuilds cheap and provides a single source for other features (e.g., marketing badges, recommendation feeds). Option B alone gives freshness but complicates caching, while Option A without incremental updates risks stale data unless we aggressively rebuild after every order.

## 7) Open Questions
- **Count definition:** Lifetime vs. rolling windows (7-day, 30-day, both?). Should counts be rounded (e.g., “1.2k”) at the API layer or left as integers?  
- **Status inclusion:** Do we count orders in `Placed`/`Preparing`, or only `Delivered`? How do we treat refunds or chargebacks?  
- **Data exposure:** Are restaurants comfortable with exact numbers, or should we bucket/threshold (e.g., show once above 20 units)?  
- **Backfill window:** For rolling windows we must decide how far back to compute (e.g., only store 90 days).  
- **Cache policy:** Should we shorten the client cache TTL or rely solely on ETag invalidation?  
- **Versioning:** Do we require the frontend to send an `Accepts-Version` header or is bumping the JSON `version` field enough?

## 8) Next Steps (Proposed)
1. Product decision on metric semantics and exposure rules.  
2. Draft schema + migration for `MenuItemSalesSummaries`.  
3. Implement maintainer + event handler for `OrderDelivered` (batching per order).  
4. Add aggregation SQL for rebuild + adjust JSON shape and version.  
5. Update tests:  
   - Functional tests for `GetFullMenuQuery` verifying sold fields populate correctly with seeded orders.  
   - Contract tests to ensure ETag/Last-Modified now reflect sales updates.  
6. Run backfill script / job for existing data.  
7. Update docs & communicate change to frontend (include sample payload).  
8. Monitor performance once enabled; tune maintenance job or indexes as needed.

## 9) Risks & Mitigations
- **High write volume on summaries:** Mitigate with batching inside event handlers (upsert aggregated per order) and ensure proper indexes.  
- **Data drift / idempotency bugs:** Store per-order watermark (`LastOrderIdProcessed`) or use `(OrderId, MenuItemId)` unique constraint to prevent double increments. Add reconciliation job that recomputes counts nightly.  
- **Large backfill:** Run in controllable batches, leverage temp tables, and monitor lock contention.  
- **API compatibility:** Adding new fields should be backward-compatible, but communicate version bump and update API docs (`Docs/API-Documentation/Customer/02-Restaurant-Discovery.md`).  
- **Cache staleness:** Verify that `LastUpdatedAt` from the summary table feeds into the ETag so downstream caches (CDN, client) invalidate when counts change.

