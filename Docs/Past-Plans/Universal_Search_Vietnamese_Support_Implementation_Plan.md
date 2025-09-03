## Universal Search — Vietnamese Support (unaccent + vi_search)

This plan details how to implement Vietnamese language support for Universal Search in our current stack (EF Core + Dapper over PostgreSQL, orchestrated with .NET Aspire using a PostGIS-enabled Postgres container).

Goals:
- Treat diacritics as equivalent (e.g., "banh mi" ≡ "bánh mì").
- Improve recall via regional synonyms (e.g., "thịt heo" ≡ "thịt lợn").
- Keep search fast and schema changes minimal; align with existing read model.

Current state:
- FTS uses the `'simple'` config with trigger-maintained `tsvector` columns (`TsAll`, `TsName`, `TsDescr`).
- Queries use `websearch_to_tsquery('simple', @q)` plus partial `ILIKE` fallback.
- Infra: `.NET Aspire` AppHost runs `postgis/postgis:16-3.4` with default PostgreSQL `tsearch_data`.

Key constraints & considerations:
- PostgreSQL’s Vietnamese support is custom: no built-in stemmer; rely on `unaccent`, custom synonyms (`thesaurus`) and stopwords (file-based).
- Synonym and stopword dictionaries require files in the container’s `tsearch_data` directory (read at server start). This affects local/dev and CI, and requires either a custom image or a bind mount.
- We must keep migrations idempotent and safe if dictionaries are not yet present (feature flag / fallback to `'simple'`).

Implementation options (trade-offs):
- Option A (Minimal, no file dictionaries):
  - Enable `unaccent` and apply it to both indexing and querying (e.g., `to_tsvector('simple', unaccent(text))`, `websearch_to_tsquery('simple', unaccent(@q))`).
  - Pros: No custom container image or volume mount; quick win for diacritics.
  - Cons: No server-side synonyms or stopwords; synonyms must be expanded in-app (query builder, via a table of synonyms) or via `keywords` column.
- Option B (Full `vi_search`):
  - Provide `vietnamese.syn` (thesaurus) and `vietnamese.stop` in `tsearch_data` and compose a `vi_search` text search configuration that chains `unaccent`, a simple+stopwords dictionary, and the thesaurus.
  - Pros: Proper language-aware behavior in-db (accent-insensitive, synonyms, stopwords) with clean SQL.
  - Cons: Requires infra work (custom image or mounts) and a migration to switch indexing/triggers and queries to `vi_search`.

Recommended path: phased rollout
1) Phase 1 — Diacritics parity via `unaccent` (no file dictionaries)
   - Enable `unaccent` extension.
   - Update trigger function to compute `Ts*` with `unaccent` on inputs (still `'simple'`).
   - Update search query to use `websearch_to_tsquery('simple', unaccent(@q))` and keep existing scoring.
   - Benefit: immediate accent-insensitive matching; minimal ops change.
2) Phase 2 — Full `vi_search` with synonyms + stopwords
   - Build/ship tsearch dictionary files; create `vi_search` configuration.
   - Migrate triggers and queries from `'simple'` to `'vi_search'`.
   - Add optional app-level synonym expansion for fallbacks and analytics.

Infra plan (Aspire + Postgres container):
- Dev/local (fast iteration): bind mount a host folder into `tsearch_data`.
  - Create repo folder `infra/postgres/tsearch_data/` containing `vietnamese.syn` and `vietnamese.stop`.
  - In `src/AppHost/Program.cs`, add a bind mount to the Postgres resource (e.g., `.WithBindMount(hostPath, "/usr/share/postgresql/16/tsearch_data")`).
  - Add an init SQL script (mounted to `/docker-entrypoint-initdb.d/`) that safely creates the dictionaries and config on first start.
- CI/prod (repeatable, stable): custom image.
  - Create `infra/postgres/Dockerfile` `FROM postgis/postgis:16-3.4`.
  - `COPY` `vietnamese.syn`, `vietnamese.stop` to the image’s `tsearch_data` (path depends on distro; typically `/usr/share/postgresql/16/tsearch_data`).
  - `COPY` an init SQL script into `/docker-entrypoint-initdb.d/10-vi-search.sql` to create `unaccent`, dictionaries, and `vi_search` config.
  - Update AppHost to use the custom image tag in all envs.

Database changes (phased):
- Phase 1 (Unaccent-only):
  - Migration A:
    - `CREATE EXTENSION IF NOT EXISTS "unaccent";`
    - Replace the existing trigger function to compute:
      - `TsName = to_tsvector('simple', unaccent(coalesce(Name,'')))`
      - `TsDescr = to_tsvector('simple', unaccent(coalesce(Description,'')))`
      - `TsAll = setweight(to_tsvector('simple', unaccent(Name)), 'A') || ...`
    - Backfill: `UPDATE "SearchIndexItems" SET "Name" = "Name";` (forces trigger to recompute vectors, as done today).
- Phase 2 (Full `vi_search`):
  - Init SQL (runs via image or mount):
    - `CREATE EXTENSION IF NOT EXISTS "unaccent";`
    - `CREATE TEXT SEARCH DICTIONARY vietnamese_simple (TEMPLATE = simple, StopWords = 'vietnamese');`
    - `CREATE TEXT SEARCH DICTIONARY thesaurus_vi (TEMPLATE = thesaurus, DictFile = 'vietnamese');`
    - `CREATE TEXT SEARCH CONFIGURATION vi_search (COPY = simple);`
    - `ALTER TEXT SEARCH CONFIGURATION vi_search ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part WITH unaccent, vietnamese_simple, thesaurus_vi;`
  - Migration B (depends on init having run successfully):
    - Guard clause: assert `vi_search` exists (or skip and log) to avoid bricking environments.
    - Replace trigger function to use `'vi_search'` in all `to_tsvector` calls.
    - Update query paths to use `websearch_to_tsquery('vi_search', @q)` (no need to call `unaccent()` explicitly if `vi_search` includes it).
    - Backfill regenerated vectors, as above.

Application changes:
- Phase 1:
  - Universal search handler: wrap `@q` with `unaccent()` in FTS predicate and ranking expression.
  - Keep `ILIKE` fallback for substring/prefix; consider `unaccent(LOWER(Name))` for parity if needed.
- Phase 2:
  - Switch from `'simple'` to `'vi_search'` in both predicate and ranking.
  - Optional: application-driven synonym expansion (OR-ing alternate phrasings) for environments that haven’t yet provisioned `vi_search`; drive by a feature flag.

Aspire orchestration specifics:
- Add a bind mount for dev: host `infra/postgres/tsearch_data` → container `.../tsearch_data`.
- Add an additional mount for init SQL: host `infra/postgres/init` → `/docker-entrypoint-initdb.d/`.
- For prod, point `.WithImage(...)` to the custom image and remove mounts.
- Ensure `POSTGRES_DB` remains set so init scripts apply to the correct DB.

Testing plan:
- Functional tests (Application.FunctionalTests):
  - Diacritics: `"banh mi"` matches rows containing `"bánh mì"` in `Name`, `Tags`, `Description`, `Cuisine`.
  - Synonyms (Phase 2): `"thit heo"` matches `"thịt lợn"` (and vice versa) via thesaurus.
  - Stopwords: queries with common stopwords behave as expected (ignored tokens don’t harm recall).
  - Non-regression: English/non-Vietnamese content remains searchable as before.
- Migration safety: test running new migrations on an existing DB snapshot and verify vectors are recomputed and indexes usable.

Rollout & ops:
- Feature flag: `Search:UseViSearch` (default false). When off, app uses `'simple'` + `unaccent`. When on and `vi_search` exists, switch to `'vi_search'`.
- Readiness probe: at startup, check for `vi_search` presence and log status; attach to health endpoint.
- Backfill job: after Phase 2 migration, run a one-time background job to touch rows in batches if table is large to spread load, or rely on the migration’s single `UPDATE` when dataset is small.

Risks & mitigations:
- Dictionary file placement differs across distros: verify path in base image; prefer custom image in prod.
- Init scripts run only on first DB initialization: for existing DBs, migrations must include `CREATE ... IF NOT EXISTS` and be idempotent.
- Query plan changes after switching configs: monitor performance; keep trigram indexes for name/cuisine to backstop recall.

Acceptance criteria:
- Phase 1: Queries without accents match accented content; functional tests pass; no regression in latency.
- Phase 2: `vi_search` configured; synonyms and stopwords verified by tests; trigger and query paths use `vi_search`; backfill complete.

Notes:
- This plan aligns with the “6) Vietnamese language support” in Next Steps and the deeper guidance in `Docs/Future-Plans/Universal_Search.md`.
- Start with Phase 1 for fast user impact; proceed to Phase 2 once infra for tsearch dictionaries is available.

---

## Phase 1 — Detailed Implementation Steps (Unaccent-only)

Objective: Make Universal Search accent-insensitive using PostgreSQL `unaccent`, without introducing custom tsearch dictionaries or a custom container image.

1) Preconditions and verification
- Verify `unaccent` availability in the running image (postgis/postgis typically ships `unaccent`).
  - Manual check (psql): `SELECT EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'unaccent');`
- Verify the application DB user can create extensions. If not, plan to run the `CREATE EXTENSION` via an init SQL under a superuser or alter DB owner temporarily.

2) Database migration A (enable and normalize vectors)
- Create a new EF Core migration: `AddUnaccentForVietnameseSearch`.
- In `Up`:
  - `CREATE EXTENSION IF NOT EXISTS "unaccent";`
  - Replace (CREATE OR REPLACE) the trigger function that maintains `TsName`, `TsDescr`, `TsAll` to apply `unaccent(...)` to all inputs while keeping the `'simple'` configuration, e.g.:
    - `TsName := to_tsvector('simple', unaccent(coalesce(NEW."Name",'')));`
    - `TsDescr := to_tsvector('simple', unaccent(coalesce(NEW."Description",'')));`
    - `TsAll := setweight(to_tsvector('simple', unaccent(coalesce(NEW."Name",''))), 'A') || ... (Cuisine, Tags, Description, Keywords with unaccent)`.
  - Ensure trigger exists (create if missing) and points to the updated function.
  - Backfill existing rows to recompute vectors (safe for small datasets): `UPDATE "SearchIndexItems" SET "Name" = "Name";`
- In `Down`:
  - Restore the previous trigger function definition without `unaccent`.
  - Optional: leave `unaccent` extension installed (safe), or drop if necessary.

3) Application query adjustments (UniversalSearchQueryHandler)
- FTS predicate change:
  - Replace `s."TsAll" @@ websearch_to_tsquery('simple', @q)` with `s."TsAll" @@ websearch_to_tsquery('simple', unaccent(@q))`.
- Ranking expression change:
  - Replace `ts_rank_cd(s."TsAll", websearch_to_tsquery('simple', @q))` with `ts_rank_cd(s."TsAll", websearch_to_tsquery('simple', unaccent(@q)))`.
- Short-query path (length <= 2):
  - Today uses `Name ILIKE @prefix`. Make prefix search accent-insensitive by comparing normalized fields:
    - `unaccent(s."Name") ILIKE unaccent(@prefix)` with `@prefix = term + '%'`.
    - Note: wrapping a column disables trigram index usage; acceptable given very short prefixes. Keep this scope limited to the short-query branch.
- Optional substring fallback (length > 2):
  - Keep existing `Name ILIKE '%' || @q || '%'` OR add a normalized fallback `OR unaccent(s."Name") ILIKE '%' || unaccent(@q) || '%'` for parity. Use only if we see mismatches in testing, to avoid unnecessary function calls.

4) Aspire and ops (Phase 1)
- No custom image or mounts required.
- Ensure migrations run with a role that can execute `CREATE EXTENSION`. If not, add a one-time init SQL file mounted to `/docker-entrypoint-initdb.d/` to create `unaccent` (dev only), or perform it out-of-band in prod.
- No change to `.WithImage(...)` needed for Phase 1.

5) Validation and testing
- Add functional tests under `tests/Application.FunctionalTests/Features/Search` to cover:
  - Diacritics matching across fields: ensure `"banh mi"` finds `Name='Bánh Mì 79'`, `Tags=['bánh mì']`, `Cuisine='Việt Nam'`, `Description` examples.
  - Short prefix behavior: term `"bá"` and `"ba"` (<= 2 chars) both return the same candidates when applicable.
  - Non-regression for English-only data.
- Validate that GIN indexes on `Ts*` are still used (via EXPLAIN in dev) and latency does not regress.

6) Rollout checklist
- Run migration on a staging database; verify extension presence and backfill duration.
- Rebuild any sample/test data if needed; smoke-test queries with/without accents.
- Monitor query latency and index usage; adjust optional normalized substring fallback if needed.

7) Acceptance for Phase 1
- Queries without accents match accented content across all FTS-covered fields.
- Short-prefix search is accent-insensitive.
- Existing facets, badges, and ranking continue to work; overall latency budgets unchanged.
