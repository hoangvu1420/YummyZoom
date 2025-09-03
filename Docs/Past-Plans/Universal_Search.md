Got it—here’s a crisp, engineering-ready deep dive on **Approach 1: Postgres-only search** for YummyZoom.

# Goals (what this approach nails)

* **Single DB** (low ops): fast v1 and strong medium-term ceiling
* **Good ranking** without extra infra
* **“Insightful & various”** results: restaurants, menu items, cuisines/tags, promos
* **Explains itself**: why an item ranked (open now, near you, promo, highly rated)

---

# Core design

## 1) Read model: `search_index_items`

A **denormalized table** that merges the key searchable bits. Built and maintained via Outbox-driven upserts. (Use a table—not a matview—for easy partial updates and per-row scoring fields.)

**Columns (suggested)**

* Identity:

  * `id UUID (PK)`, `type TEXT CHECK(type IN ('restaurant','menu_item','tag','promo'))`
  * `restaurant_id UUID NULL`
* Text fields for FTS:

  * `name TEXT`, `description TEXT NULL`, `cuisine TEXT NULL`
  * `tags TEXT[] NULL`, `keywords TEXT[] NULL` (synonyms/aliases)
  * `ts_name TSVECTOR`, `ts_descr TSVECTOR`, `ts_all TSVECTOR` *(generated or maintained in code)*
* Ranking features:

  * `is_open_now BOOLEAN`, `is_accepting_orders BOOLEAN`
  * `avg_rating DOUBLE PRECISION`, `review_count INT`
  * `has_active_promo BOOLEAN`, `price_band SMALLINT` *(1=low … 3=high)*
  * `created_at TIMESTAMPTZ`, `updated_at TIMESTAMPTZ`
* Geo:

  * `geo GEOGRAPHY(Point, 4326) NULL` *(PostGIS)*
* Ops:

  * `source_version BIGINT` *(optimistic concurrency from outbox sequence)*
  * `soft_deleted BOOLEAN DEFAULT FALSE`
  * Optional precomputed: `open_windows JSONB` (for explain), `promo_badges JSONB`

**Indexes**

```sql
-- Extensions
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS postgis;

-- FTS (GIN) 
CREATE INDEX sidx_tsv_all ON search_index_items USING GIN (ts_all);
CREATE INDEX sidx_tsv_name ON search_index_items USING GIN (ts_name);
CREATE INDEX sidx_tsv_descr ON search_index_items USING GIN (ts_descr);

-- Trigram for typo-tolerant name & cuisine
CREATE INDEX sidx_trgm_name ON search_index_items USING GIN (name gin_trgm_ops);
CREATE INDEX sidx_trgm_cuisine ON search_index_items USING GIN (cuisine gin_trgm_ops);

-- Tags (array), keywords (array)
CREATE INDEX sidx_tags_gin ON search_index_items USING GIN (tags);
CREATE INDEX sidx_keywords_gin ON search_index_items USING GIN (keywords);

-- Geo
CREATE INDEX sidx_geo ON search_index_items USING GIST ((geo));
```

**TSVECTOR population**

* Option A (preferred): **generated columns** (deterministic, zero code drift)

  ```sql
  ALTER TABLE search_index_items
  ADD COLUMN ts_all tsvector GENERATED ALWAYS AS (
    setweight(to_tsvector('simple', coalesce(name,'')), 'A') ||
    setweight(to_tsvector('simple', coalesce(array_to_string(tags,' '),'')), 'B') ||
    setweight(to_tsvector('simple', coalesce(cuisine,'')), 'B') ||
    setweight(to_tsvector('simple', coalesce(description,'')), 'C') ||
    setweight(to_tsvector('simple', coalesce(array_to_string(keywords,' '),'')), 'C')
  ) STORED;

  ALTER TABLE search_index_items
  ADD COLUMN ts_name tsvector GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce(name,''))) STORED;

  ALTER TABLE search_index_items
  ADD COLUMN ts_descr tsvector GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce(description,''))) STORED;
  ```
* Option B: maintain in code on upsert (more control for multi-dict/locale).

> **Multilingual**: For Thai + English, keep separate vectors (e.g., `ts_all_en`, `ts_all_th`) or use a composite doc + `to_tsvector('thai')` where available. At query time, OR them.

---

## 2) “Open now” calculation

You want correctness + speed. Avoid runtime parsing of business hours.

* Store **normalized weekly windows** per restaurant (e.g., `[{"dow":1,"opens":"09:00","closes":"21:00"}, ...]`) in the domain.
* Precompute **`is_open_now`** on index upsert (based on server time in UTC with restaurant TZ if needed), and also store raw windows to render explanations.
* Optionally add a SQL helper to compute fallback in DB:

  ```sql
  CREATE OR REPLACE FUNCTION is_open_now_fn(windows jsonb, now_ts timestamptz)
  RETURNS boolean AS $$
  -- simplified pseudo: parse windows for the current DOW/time range
  $$ LANGUAGE sql IMMUTABLE; -- or STABLE if now-sensitive
  ```

> In practice: compute in the indexer (AppHost background service) for deterministic results and cache.

---

## 3) Outbox → index maintenance

Use your Outbox/Inbox flow to keep `search_index_items` fresh and idempotent.

* Events: `RestaurantUpdated`, `MenuItemChanged`, `CouponChanged`, `ReviewAggregatesUpdated`.
* **Indexer handler** (derives from `IdempotentNotificationHandler<TEvent>`):

  1. Load upstream data (restaurant/menu/etc.).
  2. Map to a **single upsert** row per searchable entity.
  3. Compute `is_open_now`, `has_active_promo`, `avg_rating`, `keywords`.
  4. Upsert with `source_version` check to avoid out-of-order writes.
* Add a **full rebuild** job (one command) that truncates and repopulates from canonical tables.

**Upsert sketch**

```sql
INSERT INTO search_index_items (id, type, restaurant_id, name, description, cuisine, tags, keywords,
    is_open_now, is_accepting_orders, avg_rating, review_count, has_active_promo, price_band,
    geo, created_at, updated_at, source_version, soft_deleted)
VALUES (@Id, @Type, @RestaurantId, @Name, @Description, @Cuisine, @Tags, @Keywords,
    @IsOpenNow, @IsAcceptingOrders, @AvgRating, @ReviewCount, @HasActivePromo, @PriceBand,
    ST_GeogFromText(@WktPoint), @CreatedAt, @UpdatedAt, @Version, @SoftDeleted)
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    cuisine = EXCLUDED.cuisine,
    tags = EXCLUDED.tags,
    keywords = EXCLUDED.keywords,
    is_open_now = EXCLUDED.is_open_now,
    is_accepting_orders = EXCLUDED.is_accepting_orders,
    avg_rating = EXCLUDED.avg_rating,
    review_count = EXCLUDED.review_count,
    has_active_promo = EXCLUDED.has_active_promo,
    price_band = EXCLUDED.price_band,
    geo = EXCLUDED.geo,
    updated_at = EXCLUDED.updated_at,
    soft_deleted = EXCLUDED.soft_deleted
WHERE search_index_items.source_version <= EXCLUDED.source_version;
```

---

## 4) Querying (Dapper) — recall + rank + diversify

### Inputs

* `q` (text), `userLat/Lon` (optional), filters: `openNow`, `cuisines[]`, `tags[]`, `priceBand`, `promoOnly`, pagination.

### Recall strategy

* Primary: FTS on `ts_all` using `plainto_tsquery` or `websearch_to_tsquery` for natural syntax.
* Backoff: `ILIKE` + trigram on `name`/`cuisine` for typos or very short queries.
* If `q` empty: return “discovery”: best nearby open restaurants + facets.

### Ranking formula (explainable, SQL-native)

* `text_score`: `ts_rank_cd(ts_all, query)` (scaled 0..1 per bucket)
* `open_boost`: `CASE WHEN is_open_now AND is_accepting_orders THEN 1 ELSE 0 END`
* `rating_score`: `LEAST(1, avg_rating/5.0) * LOG(1+review_count)`
* `promo_boost`: `CASE WHEN has_active_promo THEN 1 ELSE 0 END`
* `distance_score`: `1 / (1 + (ST_Distance(geo, userPoint) / 1000.0))` *(km decay; clamp)*
* Blend:

  ```sql
  score =
    0.45*text_score +
    0.15*distance_score +
    0.15*rating_score +
    0.15*open_boost +
    0.10*promo_boost
  ```

### Example SQL (single pass)

```sql
WITH params AS (
  SELECT
    websearch_to_tsquery('simple', @q) AS q,
    ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)::geography AS user_geo,
    @open_now::bool AS open_now_only,
    @cuisines::text[] AS cuisines,
    @tags::text[] AS tags
),
base AS (
  SELECT sii.*,
         ts_rank_cd(sii.ts_all, p.q) AS text_score,
         CASE WHEN p.user_geo IS NOT NULL AND sii.geo IS NOT NULL
              THEN 1.0 / (1.0 + (ST_Distance(sii.geo, p.user_geo) / 1000.0))
              ELSE 0.5 END AS distance_score,
         (LEAST(1.0, COALESCE(avg_rating,0)/5.0) * LOG(1+COALESCE(review_count,0))) AS rating_score,
         CASE WHEN is_open_now AND is_accepting_orders THEN 1.0 ELSE 0.0 END AS open_boost,
         CASE WHEN has_active_promo THEN 1.0 ELSE 0.0 END AS promo_boost
  FROM search_index_items sii
  CROSS JOIN params p
  WHERE sii.soft_deleted = FALSE
    AND (
      p.q IS NULL
      OR sii.ts_all @@ p.q
      OR sii.name ILIKE '%' || @q || '%'
    )
    AND (p.open_now_only IS FALSE OR (sii.is_open_now AND sii.is_accepting_orders))
    AND (p.cuisines IS NULL OR sii.cuisine = ANY(p.cuisines))
    AND (p.tags IS NULL OR EXISTS (SELECT 1 FROM unnest(sii.tags) t WHERE t = ANY(p.tags)))
),
scored AS (
  SELECT *,
    (0.45*text_score + 0.15*distance_score + 0.15*rating_score + 0.15*open_boost + 0.10*promo_boost) AS score
  FROM base
)
SELECT *
FROM scored
ORDER BY score DESC, updated_at DESC
OFFSET @offset LIMIT @limit;
```

> **Diversity**: After ranking, apply a lightweight **“round-robin by type”** or “cap per type” in code (e.g., top 5 restaurants, top 7 menu items, 3 tags, 2 promos) to ensure variety.

### Highlights (insightful results)

* Use `ts_headline` to return emphasized snippets:

  ```sql
  ts_headline('simple', description, q, 'ShortWord=2, MaxFragments=2, MinWords=5, MaxWords=12')
  ```
* Server synthesizes **explanations** per item:

  * “Open now”, “\~12–18 min away”, “⭐ 4.6 (1.1k)”, “20% off”, “Popular in Thai cuisine”

### Facets (for filters UI)

Compute with **aggregations** on the same filtered set:

```sql
SELECT cuisine, COUNT(*) FROM base WHERE type='restaurant' GROUP BY cuisine ORDER BY COUNT(*) DESC LIMIT 15;
SELECT unnest(tags) AS tag, COUNT(*) FROM base WHERE type IN ('menu_item','restaurant') GROUP BY tag ORDER BY COUNT(*) DESC LIMIT 20;
SELECT price_band, COUNT(*) FROM base WHERE type IN ('menu_item','restaurant') GROUP BY price_band;
```

---

## 5) Autocomplete (as-you-type)

Separate endpoint optimized for sub-200ms latency.

**Recall**

* Prefix match on normalized `name` (`name ILIKE @prefix || '%'`)
* Trigram similarity for misspellings:

  ```sql
  SELECT id, type, name
  FROM search_index_items
  WHERE similarity(name, @q) > 0.3
  ORDER BY greatest(similarity(name,@q),
                   CASE WHEN name ILIKE @q || '%' THEN 1 ELSE 0 END) DESC
  LIMIT 10;
  ```
* Include curated suggestions from a small `popular_queries` table (rotate daily from click logs).

**Return**

* A mix of entity suggestions (restaurants + items) and **query suggestions** (e.g., “vegan thai”, “under 200฿”).

---

## 6) API & Application layer shape

**Interface**

```csharp
public interface ISearchProvider {
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct);
    Task<IReadOnlyList<SuggestionDto>> AutocompleteAsync(string q, CancellationToken ct);
}
```

**Handler (Dapper)**

* Parses input → builds SQL with parameterized filters
* Executes main query + highlights + facets in a single connection (multiple commands)
* Post-processes:

  * **Type diversification**
  * Adds **explanations**
  * Formats **distance/ETA** (if you compute ETA in app, use distance to look up a delivery-zone curve)

**DTO essentials**

* For each hit: `Id`, `Type`, `RestaurantId`, `Name`, `Snippet`, `Badges[]`, `Score`, `Highlights[]`, `DistanceKm?`, `EtaRange?`.

---

## 7) Relevance iteration & safety rails

* **Click/convert logging**: capture search session id, positions, clicks, order conversions.
* **A/B tunables**: weights for `text/rating/distance/open/promo` exposed via config.
* **Guardrails**:

  * Always filter out `soft_deleted = TRUE`.
  * Respect `IsAcceptingOrders`.
  * Down-rank unavailable menu items (or hide).

---

## 8) Caching & performance

* **Autocomplete**: cache by prefix (TTL 30–60s).
* **Full search**: cache only for popular queries without user-specific geo/filters (short TTL).
* Use **prepared statements** in Dapper and keep queries stable.
* Paginate (PageSize ≤ 20), measure P95.

---

## 9) Testing checklist

* **Functional**: typos (“krapow” vs “kaprao”), multilingual, tag/cuisine filters, open-now edge at boundaries.
* **Deterministic**: “drain outbox → query → expect stable ranking order given fixed weights”.
* **Load**: autocomplete under concurrent keystrokes.
* **Rebuild**: full rebuild parity with online incremental.

---

### TL;DR (what to implement first)

1. Create `search_index_items` with FTS/trigram/geo indexes.
2. Outbox-driven **indexer upserts** with `is_open_now`, `has_active_promo`, ratings, geo.
3. Dapper **search query** using `websearch_to_tsquery`, rank blend, `ts_headline`, and facets.
4. **Autocomplete** endpoint with prefix + trigram + curated suggestions.
5. Lightweight **diversification** and **explanations** in the response.

=========











Here’s a **clean, comprehensive spec** for the **Postgres read model (Approach 1)**—focused on the **table schema** and the **indexes**, plus why each exists and the query shapes they accelerate.

# 1) Table: `search_index_items` (read model)

**Goal:** a single denormalized table, one row per searchable entity (restaurant, menu item, cuisine/tag, promo), optimized for:

* full-text recall,
* fast boolean/attribute filters,
* geo filtering/ranking,
* explainable scoring,
* safe, idempotent upserts from your Outbox.

```sql
-- One-time extensions (at the database level)
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS postgis;

CREATE TABLE IF NOT EXISTS search_index_items (
  -- Identity / relations
  id                  uuid PRIMARY KEY,
  type                text NOT NULL CHECK (type IN ('restaurant','menu_item','tag','promo')),
  restaurant_id       uuid NULL,                   -- logical backref; no FK to keep read model decoupled

  -- Displayable text
  name                text NOT NULL,
  description         text NULL,

  -- Classification
  cuisine             text NULL,                   -- normalized (“thai”, “japanese”)
  tags                text[] NULL,                 -- normalized, lowercased
  keywords            text[] NULL,                 -- synonyms/aliases (e.g., “kaprao”, “krapow”)

  -- Availability / economics / quality
  is_open_now         boolean NOT NULL DEFAULT false,
  is_accepting_orders boolean NOT NULL DEFAULT false,
  has_active_promo    boolean NOT NULL DEFAULT false,
  price_band          smallint NULL,               -- 1=low, 2=mid, 3=high
  avg_rating          double precision NULL,       -- 0..5 (NULL when unknown)
  review_count        integer NOT NULL DEFAULT 0,

  -- Geo (meters; great for ST_DWithin & ST_Distance)
  geo                 geography(Point, 4326) NULL,

  -- Ops & consistency
  created_at          timestamptz NOT NULL DEFAULT now(),
  updated_at          timestamptz NOT NULL DEFAULT now(),
  source_version      bigint NOT NULL DEFAULT 0,   -- monotonic from Outbox to gate upserts
  soft_deleted        boolean NOT NULL DEFAULT false,

  -- Data for UI “explanations” (not used in joins)
  open_windows        jsonb NULL,                  -- normalized weekly schedule for badges
  promo_badges        jsonb NULL,                  -- e.g., [{"label":"-20%","ends":"2025-09-01"}]

  -- Generated FTS vectors (baseline: single dictionary; see multilingual below)
  ts_name tsvector GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce(name,''))) STORED,

  ts_descr tsvector GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce(description,''))) STORED,

  ts_tags tsvector GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce(array_to_string(tags,' '),''))) STORED,

  ts_all tsvector GENERATED ALWAYS AS (
      setweight(to_tsvector('simple', coalesce(name,'')), 'A')
    || setweight(to_tsvector('simple', coalesce(cuisine,'')), 'B')
    || setweight(to_tsvector('simple', coalesce(array_to_string(tags,' '),'')), 'B')
    || setweight(to_tsvector('simple', coalesce(description,'')), 'C')
    || setweight(to_tsvector('simple', coalesce(array_to_string(keywords,' '),'')), 'C')
  ) STORED
);
```

### Why these columns

* **`type`** lets you diversify results (restaurants vs items vs promos).
* **`keywords`** gives you recall over common misspellings / transliterations without heavy synonym logic.
* **`source_version`** prevents stale/out-of-order writes from the Outbox indexer.
* **Generated `tsvector`s** keep FTS up-to-date automatically; no app code needed to maintain vectors.
* **`geo` geography** supports accurate distance (meters) and radius filters.

---

# 2) Indexes (what to create and why)

### Full-text recall (main path)

```sql
-- Broad recall across all text with weights (primary FTS index)
CREATE INDEX sidx_tsv_all   ON search_index_items USING GIN (ts_all);

-- Optional: targeted FTS for ultra-fast "name-only" or "description-only" queries
CREATE INDEX sidx_tsv_name  ON search_index_items USING GIN (ts_name);
CREATE INDEX sidx_tsv_descr ON search_index_items USING GIN (ts_descr);
```

**Why:** `GIN` on `tsvector` is ideal for `@@ websearch_to_tsquery(...)`. `ts_all` is enough for most queries; `ts_name` can speed exact-name lookups and boost precision for very short queries.

---

### Typo tolerance & autocomplete backoff

```sql
-- Trigram (fuzzy + prefix-ish ranking)
CREATE INDEX sidx_trgm_name    ON search_index_items USING GIN (name gin_trgm_ops);
CREATE INDEX sidx_trgm_cuisine ON search_index_items USING GIN (cuisine gin_trgm_ops);
```

**Why:** When FTS misses short or misspelled inputs, trigram similarity (`similarity(name, :q)`) gives robust fallback and powers autocomplete suggestions.

---

### Arrays (tags / keywords)

```sql
CREATE INDEX sidx_tags_gin     ON search_index_items USING GIN (tags);
CREATE INDEX sidx_keywords_gin ON search_index_items USING GIN (keywords);
```

**Why:** Speeds `tags && :tags` or `EXISTS (t = ANY(tags))` filters, and keyword recall without scanning.

---

### Geo (radius + distance)

```sql
CREATE INDEX sidx_geo ON search_index_items USING GIST (geo);
```

**Why:** `GiST` on `geography` accelerates `ST_DWithin(geo, :pt, :m)` and prunes distance sorts. (If you don’t use PostGIS, skip this and store `lat/lon` as doubles—less accurate, fewer features.)

---

### Common WHERE predicates (btree)

```sql
CREATE INDEX sidx_type_open    ON search_index_items (type, is_open_now, is_accepting_orders);
CREATE INDEX sidx_type_promo   ON search_index_items (type, has_active_promo);
CREATE INDEX sidx_soft_deleted ON search_index_items (soft_deleted);
CREATE INDEX sidx_updated_at   ON search_index_items (updated_at DESC);
```

**Why:** Cheap, selective filters that appear in nearly every query; `updated_at` helps stable tie-breaks and admin views.

---

### Optional partial indexes (when data is skewed)

```sql
CREATE INDEX sidx_open_now_only ON search_index_items (type, updated_at DESC)
  WHERE is_open_now AND is_accepting_orders AND NOT soft_deleted;

CREATE INDEX sidx_promos_only   ON search_index_items (type, updated_at DESC)
  WHERE has_active_promo AND NOT soft_deleted;
```

**Why:** If only a minority is “open now” or “on promo,” these smaller indexes stay hot and reduce scans when those filters are used.

---

# 3) Multilingual FTS (Thai + English)

If your content/queries are bilingual, keep **two vectors** and OR them at query time:

```sql
ALTER TABLE search_index_items
ADD COLUMN ts_all_en tsvector GENERATED ALWAYS AS (
  setweight(to_tsvector('english', coalesce(name,'')), 'A')
  || setweight(to_tsvector('english', coalesce(description,'')), 'C')
  || setweight(to_tsvector('english', coalesce(array_to_string(tags,' '),'')), 'B')
  || setweight(to_tsvector('english', coalesce(cuisine,'')), 'B')
  || setweight(to_tsvector('english', coalesce(array_to_string(keywords,' '),'')), 'C')
) STORED,
ADD COLUMN ts_all_th tsvector GENERATED ALWAYS AS (
  -- Replace 'simple' with a Thai dictionary if you deploy one
  setweight(to_tsvector('simple', coalesce(name,'')), 'A')
  || setweight(to_tsvector('simple', coalesce(description,'')), 'C')
  || setweight(to_tsvector('simple', coalesce(array_to_string(tags,' '),'')), 'B')
  || setweight(to_tsvector('simple', coalesce(cuisine,'')), 'B')
  || setweight(to_tsvector('simple', coalesce(array_to_string(keywords,' '),'')), 'C')
) STORED;

CREATE INDEX sidx_tsv_all_en ON search_index_items USING GIN (ts_all_en);
CREATE INDEX sidx_tsv_all_th ON search_index_items USING GIN (ts_all_th);
```

**Query shape:**
`WHERE (ts_all_en @@ websearch_to_tsquery('english', :q) OR ts_all_th @@ websearch_to_tsquery('simple', :q))`

---

# 4) Query shapes → which index is used

* **Main search**
  `... WHERE ts_all @@ websearch_to_tsquery(:q)` → `sidx_tsv_all`
  (Then sort with a blended score; use `updated_at` as a secondary tie-break.)

* **Autocomplete / typo backoff**
  `... WHERE similarity(name,:q) > 0.3 ORDER BY similarity(name,:q) DESC` → `sidx_trgm_name`

* **Open now / accepting**
  `... WHERE is_open_now AND is_accepting_orders` → `sidx_type_open` (or partial)

* **Promos**
  `... WHERE has_active_promo` → `sidx_type_promo` (or partial)

* **Cuisine / tags**
  `... WHERE cuisine = ANY(:cuisines)` → `sidx_trgm_cuisine` can help LIKEs;
  `... WHERE tags && :tags` → `sidx_tags_gin`

* **Geo**
  `... WHERE ST_DWithin(geo, :user_geo, :meters)` → `sidx_geo`
  (Then compute `ST_Distance` for ranking; index prunes candidates.)

---

# 5) Upserts (idempotent) & FTS maintenance

Vectors are **generated columns**, so you only upsert the raw text/arrays—Postgres keeps vectors in sync.

```sql
INSERT INTO search_index_items (id, type, restaurant_id, name, description, cuisine, tags, keywords,
  is_open_now, is_accepting_orders, has_active_promo, price_band,
  avg_rating, review_count, geo,
  created_at, updated_at, source_version, soft_deleted,
  open_windows, promo_badges)
VALUES ( ... )
ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  cuisine = EXCLUDED.cuisine,
  tags = EXCLUDED.tags,
  keywords = EXCLUDED.keywords,
  is_open_now = EXCLUDED.is_open_now,
  is_accepting_orders = EXCLUDED.is_accepting_orders,
  has_active_promo = EXCLUDED.has_active_promo,
  price_band = EXCLUDED.price_band,
  avg_rating = EXCLUDED.avg_rating,
  review_count = EXCLUDED.review_count,
  geo = EXCLUDED.geo,
  updated_at = EXCLUDED.updated_at,
  soft_deleted = EXCLUDED.soft_deleted,
  open_windows = EXCLUDED.open_windows,
  promo_badges = EXCLUDED.promo_badges,
  source_version = EXCLUDED.source_version
WHERE search_index_items.source_version <= EXCLUDED.source_version;
```

---

# 6) Practical maintenance knobs (quick)

* **Autovacuum**: upserts + boolean flips (“open now”) produce churn; keep autovacuum aggressive on this table.
* **Fillfactor**: `ALTER TABLE search_index_items SET (fillfactor=80);` to reduce page splits under frequent updates.
* **GIN fastupdate**: keep on for day-to-day; for bulk rebuilds, consider `SET fastupdate = off` on the GIN indexes, load, then `REINDEX CONCURRENTLY`.
* **Stats**: if you filter heavily by `cuisine`/`tags`, raise stats target:

  ```sql
  ALTER TABLE search_index_items ALTER COLUMN tags SET STATISTICS 500;
  ALTER TABLE search_index_items ALTER COLUMN cuisine SET STATISTICS 500;
  ```

---

## TL;DR

* **One denormalized table** with **generated FTS vectors** and **geography point**.
* **Indexes**:

  * GIN on `ts_all` (+ optional name/descr),
  * trigram on `name`/`cuisine`,
  * GIN on `tags`/`keywords`,
  * GiST on `geo`,
  * small btrees for common booleans & `updated_at`,
  * optional **partial** indexes for “open now” and “promos”.
* **Multilingual**: add `ts_all_en` and `ts_all_th` with their own GINs and OR them in WHERE.
* **Upserts** gated by `source_version` keep the index consistent with Outbox events.

===================















Here is a breakdown of the challenges and the specific PostgreSQL solutions to address them, making the search robust for Vietnamese.

---

### Key Challenges and Solutions for Vietnamese Search

#### 1. Challenge: Diacritics (Accents)
Vietnamese uses a rich set of diacritics (e.g., `a`, `á`, `à`, `ả`, `ã`, `ạ`). Users often type without these accents for convenience. The search engine must treat `banh mi` and `bánh mì` as equivalent queries.

**✅ Solution: Use the `unaccent` Extension**

PostgreSQL's `unaccent` extension is designed for exactly this purpose. It removes accents from text. We will create a custom text search configuration that includes `unaccent` in its processing pipeline.

#### 2. Challenge: Tokenization & Synonyms
Vietnamese is an analytic language, meaning words are generally not inflected (no stemming like "run" vs. "running"). Tokenization by whitespace is often sufficient. However, synonyms are very common. For example, in different regions of Vietnam, "pork" can be `thịt heo` (southern) or `thịt lợn` (northern). The search should understand these are the same.

**✅ Solution: Create a Vietnamese Synonym Dictionary**

We can create a thesaurus file that maps synonyms. This allows the search engine to expand a query for `thịt heo` to also include results for `thịt lợn`, dramatically improving recall.

#### 3. Challenge: Stop Words
Like any language, Vietnamese has common "stop words" (`là`, `và`, `của` - is, and, of) that add noise to searches and should be ignored.

**✅ Solution: Use a Vietnamese Stop Word List**

We will provide a list of common Vietnamese words to be filtered out during the indexing process.

---

### Step-by-Step Implementation Plan

Here’s how we adapt the proposed schema and configuration for Vietnamese.

#### Step 1: Install Extension and Create Configuration Files

First, ensure the `unaccent` extension is available. Then, an administrator would create two files on the database server.

```sql
-- Run this once per database
CREATE EXTENSION IF NOT EXISTS "unaccent";
```

**File 1: `vietnamese.syn` (Synonym File)**
This file defines synonym groups.

```txt
# /path/to/postgresql/data/tsearch_data/vietnamese.syn
thịt heo : thịt lợn
bông thiên lý : hoa thiên lý
trái thơm : trái dứa
```

**File 2: `vietnamese.stop` (Stop Word File)**
This file lists words to ignore.

```txt
# /path/to/postgresql/data/tsearch_data/vietnamese.stop
và
của
là
cái
một
cho
không
...
```

#### Step 2: Create a Custom Vietnamese Text Search Configuration

Now, we use SQL to define a new text search configuration that ties these pieces together.

```sql
-- 1. Create a thesaurus dictionary using our synonym file
CREATE TEXT SEARCH DICTIONARY thesaurus_vi (
    TEMPLATE = thesaurus,
    DictFile = vietnamese, -- Corresponds to vietnamese.syn
    Dictionary = simple
);

-- 2. Create a dictionary that uses the 'simple' template (no stemming) but with a stop word list
CREATE TEXT SEARCH DICTIONARY vietnamese_simple (
    TEMPLATE = simple,
    StopWords = vietnamese -- Corresponds to vietnamese.stop
);

-- 3. Create the final, comprehensive Vietnamese configuration
CREATE TEXT SEARCH CONFIGURATION vi_search (COPY = simple);

-- 4. Alter the configuration to use our custom dictionaries in the correct order
ALTER TEXT SEARCH CONFIGURATION vi_search
    ALTER MAPPING FOR asciiword, hword_asciipart, word, hword_part, hword
    WITH unaccent, vietnamese_simple, thesaurus_vi;
```

**Explanation of the `ALTER MAPPING` command:**
When PostgreSQL processes text for this configuration, it will:
1.  First, pass the token through `unaccent` (e.g., `bánh` → `banh`).
2.  Then, pass the result through `vietnamese_simple` to check if it's a stop word.
3.  Finally, pass it through `thesaurus_vi` to check for synonyms.

#### Step 3: Update the `search_index_items` DDL

We now update the table definition to use our new `vi_search` configuration instead of `'simple'`.

```sql
CREATE TABLE IF NOT EXISTS search_index_items (
    -- ... (all other columns remain the same) ...

    -- Generated FTS columns now use the 'vi_search' configuration
    ts_name tsvector GENERATED ALWAYS AS (
        to_tsvector('vi_search', coalesce(name,''))
    ) STORED,

    ts_descr tsvector GENERATED ALWAYS AS (
        to_tsvector('vi_search', coalesce(description,''))
    ) STORED,

    ts_tags tsvector GENERATED ALWAYS AS (
        to_tsvector('vi_search', coalesce(array_to_string(tags,' '),''))
    ) STORED,

    ts_all tsvector GENERATED ALWAYS AS (
        setweight(to_tsvector('vi_search', coalesce(name,'')), 'A') ||
        setweight(to_tsvector('vi_search', coalesce(cuisine,'')), 'B') ||
        setweight(to_tsvector('vi_search', coalesce(array_to_string(tags,' '),'')), 'B') ||
        setweight(to_tsvector('vi_search', coalesce(description,'')), 'C') ||
        setweight(to_tsvector('vi_search', coalesce(array_to_string(keywords,' '),'')), 'C')
    ) STORED
);
```

#### Step 4: Update the Application Query Logic

Finally, the query handler in your application layer must also specify the `vi_search` configuration when creating the search query.

```csharp
// Inside the UniversalSearchQueryHandler
// The query function must also use the correct language configuration
var ftsQuery = "websearch_to_tsquery('vi_search', @Term)";

var sql = $"""
SELECT
    -- ...
    ts_rank_cd("ts_all", {ftsQuery}) AS fts_rank,
    -- ...
FROM "search_index_items"
WHERE
    "ts_all" @@ {ftsQuery}
    -- ...
""";
```

### Conclusion

The proposed search design is not only compatible with Vietnamese but is highly adaptable to it. By replacing the generic `'simple'` FTS configuration with a custom `'vi_search'` configuration that leverages the `unaccent` extension and custom synonym/stop word lists, you create a powerful, language-aware search experience.

This approach provides a robust solution that correctly handles the nuances of the Vietnamese language, ensuring that users can find what they're looking for, regardless of whether they use diacritics or regional synonyms.

======================


















Of course. Based on our detailed discussion, here is a comprehensive, step-by-step implementation plan to build the universal search functionality in the YummyZoom project.

This plan follows your established Clean Architecture, CQRS patterns, and project structure.

---

### **Universal Search Implementation Plan**

#### **Phase 0: Prerequisites & Foundation Setup**

*Goal: Prepare the database and project environment for the new search functionality.*

1.  **Database Administrator Task: Prepare PostgreSQL Server**
    *   **Action:** Install the Vietnamese synonym (`vietnamese.syn`) and stop word (`vietnamese.stop`) files in the PostgreSQL `tsearch_data` directory.
    *   **Action:** Create the custom text search configuration `vi_search` using the provided SQL script that leverages the `unaccent` extension and the new dictionary files.
    *   **Rationale:** These server-side objects must exist before the application migration can use them.

2.  **Developer Task: Update Project Dependencies**
    *   **Action:** Add the `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` NuGet package to the `Infrastructure` project.
    *   **Rationale:** Required for handling the `geography(Point)` data type with EF Core.

3.  **Developer Task: Audit & Enhance Domain Events**
    *   **Action:** Review the `Restaurant`, `MenuItem`, `Coupon`, and other relevant aggregates.
    *   **Action:** Ensure that their domain events (e.g., `RestaurantProfileUpdated`, `MenuItemAdded`, `CouponActivated`) contain all the necessary data for the search index (name, description, price, location, status flags, etc.).
    *   **Action:** Crucially, add a `long Version` or `long SourceVersion` property to these events to be used for idempotent updates in the read model.
    *   **Location:** `src/Domain/<FeatureName>/Events/`

---

#### **Phase 1: Build the Search Read Model (Write Side)**

*Goal: Create the database schema and the data pipeline to populate it.*

1.  **Define the Read Model Schema in C#**
    *   **Action:** Create the `SearchIndexItem.cs` class in `src/Infrastructure/Persistence/ReadModels/`. This class defines the columns of our search table but is not a domain entity.
    *   **Action:** Create the `SearchIndexItemEntityTypeConfiguration.cs` configuration file in `src/Infrastructure/Persistence/Configurations/`.
    *   **Action:** Implement the Fluent API configuration as discussed, using `HasComputedColumnSql` for `tsvector` fields, and `HasMethod`, `ForNpgsqlHasOperators`, and `HasFilter` to define all specialized indexes (GIN, GIST, Partial).
    *   **Action:** Add `public DbSet<SearchIndexItem> SearchIndexItems { get; set; }` to `ApplicationDbContext.cs`.
    *   **Action:** In `DbContext.OnModelCreating`, add `modelBuilder.HasPostgresExtension(...)` for `pg_trgm`, `postgis`, and `unaccent`.

2.  **Generate and Verify the Database Migration**
    *   **Action:** Run `dotnet ef migrations add CreateSearchIndexItemsReadModel`.
    *   **Action:** **Carefully review the generated migration file.** Verify that it correctly creates the extensions, the table, the `GENERATED ALWAYS AS` columns with the `vi_search` configuration, and all the specialized indexes.

3.  **Implement the Data Projection Logic (Event Handlers)**
    *   **Action:** Create a new feature folder: `src/Infrastructure/Search/`.
    *   **Action:** Inside, create a repository interface `ISearchIndexItemRepository.cs` and its Dapper-based implementation `SearchIndexItemRepository.cs`. This repository will contain a single, highly-optimized `UpsertAsync` method using the `INSERT ... ON CONFLICT ... DO UPDATE` pattern, which checks the `source_version`.
    *   **Action:** Create a subfolder `EventHandlers/`. For each domain event that affects search, implement a corresponding handler (e.g., `RestaurantProfileUpdatedSearchHandler.cs`, `MenuItemAddedSearchHandler.cs`).
    *   **Action:** These handlers must inherit from the project's `IdempotentNotificationHandler<TEvent>` to ensure exactly-once processing. Their `HandleCore` method will transform the event data into a `SearchIndexItem` and call `_searchIndexItemRepository.UpsertAsync()`.

---

#### **Phase 2: Implement the Search Query (Read Side)**

*Goal: Create the CQRS query and handler to efficiently retrieve search results.*

1.  **Define the Application Layer Components**
    *   **Action:** Create a new feature folder: `src/Application/Search/`.
    *   **Action:** Create the query record `UniversalSearchQuery.cs`. It should include parameters for `string Term`, `double? Latitude`, `double? Longitude`, filter flags (`bool IsOpenNow`, etc.), and pagination (`int PageNumber`, `int PageSize`).
    *   **Action:** Create the response DTO `SearchResultDto.cs`. This DTO should be a flat object containing everything the UI needs to display a single search result.
    *   **Action:** Create the query handler `UniversalSearchQueryHandler.cs`.

2.  **Implement the High-Performance Query Handler**
    *   **Action:** Inject `IDbConnectionFactory` into the handler.
    *   **Action:** Use Dapper to execute a raw SQL query.
    *   **Action:** The query must use `websearch_to_tsquery('vi_search', @Term)` to leverage the Vietnamese FTS configuration.
    *   **Action:** Dynamically build the `WHERE` clause based on the query's filter parameters.
    *   **Action:** Construct a multi-factor `ORDER BY` clause that combines `ts_rank_cd` (text relevance), `similarity` (typo tolerance), business metrics (`avg_rating`), and geospatial distance (`ST_Distance`).
    *   **Action:** The handler should return a `Result<PaginatedList<SearchResultDto>>`.

---

#### **Phase 3: Expose the Functionality via API**

*Goal: Create a user-facing endpoint for the search feature.*

1.  **Create the API Endpoint**
    *   **Action:** In the `src/Web/` project, create a new `SearchController.cs`.
    *   **Action:** Add a `GET` endpoint, e.g., `[HttpGet("/api/search")]`.
    *   **Action:** The endpoint method will accept parameters from the query string (`[FromQuery]`) that map to the `UniversalSearchQuery`.
    *   **Action:** The controller will create an instance of `UniversalSearchQuery`, send it via the `ISender` (MediatR), and return the result.

---

#### **Phase 4: Testing & Validation**

*Goal: Ensure the search functionality is correct, performant, and reliable.*

1.  **Infrastructure Integration Tests**
    *   **Action:** In `tests/Infrastructure.IntegrationTests/`, write tests for the event handlers.
    *   **Test Case:** Given a specific domain event, assert that the handler correctly creates or updates the corresponding record in the `search_index_items` table with the correct transformed data.

2.  **Application Functional Tests (End-to-End)**
    *   **Action:** In `tests/Application.FunctionalTests/`, write comprehensive tests for the entire search flow.
    *   **Test Scenario 1 (Relevance):**
        1.  Create a Restaurant "Phở Thìn Lò Đúc" and a menu item "Bún Bò Huế".
        2.  Drain the outbox (`Testing.DrainOutboxAsync()`).
        3.  Send a `UniversalSearchQuery` with `Term = "pho bo"`.
        4.  Assert that "Phở Thìn Lò Đúc" is ranked higher than "Bún Bò Huế".
    *   **Test Scenario 2 (Filtering):**
        1.  Create two restaurants, one with `is_open_now = true`, one with `false`.
        2.  Drain the outbox.
        3.  Send a query with `IsOpenNow = true`.
        4.  Assert that only the open restaurant is returned.
    *   **Test Scenario 3 (Vietnamese Support):**
        1.  Create a restaurant named "Bánh Mì Huynh Hoa".
        2.  Drain the outbox.
        3.  Send a query with `Term = "banh mi"` (no diacritics).
        4.  Assert that the restaurant is found.

---

#### **Phase 5: Deployment & Maintenance**

*Goal: Successfully deploy the feature and establish a process for its ongoing improvement.*

1.  **Deployment Checklist**
    *   Ensure the DBA tasks from Phase 0 are completed on the production database server *before* deploying the application.
    *   The application deployment will run the new EF Core migration, creating the search schema.
    *   Plan for an initial backfill/re-indexing job to populate the `search_index_items` table with data from existing entities. This can be a one-off script or a temporary hosted service.

2.  **Maintenance Plan**
    *   **Action:** Establish a recurring process (e.g., quarterly) to analyze application search logs for common queries that yield zero results.
    *   **Action:** Use these insights to update the `vietnamese.syn` file with new synonyms to continuously improve search recall.
    *   **Action:** Monitor search query performance using a tool like pg_stat_statements and ensure the indexes are being used effectively.