## Universal Search – Next Steps (Post‑MVP)

This proposal prioritizes the highest user‑experience (UX) value enhancements for the Universal Search and Autocomplete features, informed by the deferred items in `Universal_Search_MVP.md` and the broader roadmap in `Universal_Search.md`.

### Guiding Principles
- Focus on fast, relevant, and explainable results
- Minimize user effort with smart defaults, forgiving input, and helpful navigation
- Ship value incrementally with measurable impact and low operational risk

---

### Phase 1 — Immediate UX Wins (High impact, low/medium effort)

1) Faceted filters (cuisine, tags, price, open‑now)
- What: Add facet aggregations to the search response and expose simple filter chips (cuisine top‑N, tags top‑N, price bands, open‑now toggle).
- Why (UX): Users quickly refine large result sets without retyping queries.
- Acceptance:
  - Response returns facet counts for the current filtered set
  - UI can apply multiple facets and see counts update
  - Performance: < 250ms p95 on typical datasets
- Notes: Query adds lightweight GROUP BYs on the already filtered base CTE (see `Universal_Search.md`).

2) Result explanations and badges
- What: Return lightweight badges and explanations per hit: “Open now”, “Near you”, “⭐ 4.6 (1.1k)”, “Promo 20%”.
- Why (UX): Builds trust and helps users scan/compare quickly.
- Acceptance:
  - Each result includes `Badges[]` and a short `Reason` string
  - Badge logic is deterministic and documented
- Notes: Compute from existing fields (is_open_now, distance, rating, promo flags). No UI changes to ranking yet.

3) Better highlighting/snippets
- What: Use `ts_headline` for matched fragments in descriptions; bold matched terms in names.
- Why (UX): Improves scannability and understanding of why an item matched.
- Acceptance: Results include `DescriptionSnippet` with highlights for matched terms; names show inline emphasis when applicable.

4) Zero‑results guidance and graceful fallback
- What: When no results, show helpful suggestions: broaden filters, nearby cuisines, popular searches; relax matching to trigram fallback.
- Why (UX): Avoid dead ends and reduce abandonment.
- Acceptance: For zero results, response includes a populated `Suggestions` block and a relaxed recall attempt.

5) Autocomplete UX polish
- What: Mix entity suggestions with curated query suggestions; boost prefix matches; trim latency with short TTL cache by prefix.
- Why (UX): Faster selection, fewer keystrokes, better discovery.
- Acceptance: Top‑10 suggestions combine entities and curated queries; p95 latency < 120ms with in‑memory/Redis prefix cache.

---

### Phase 2 — Relevance & Recall Boosters (High impact, medium effort)

6) Vietnamese language support (unaccent + `vi_search` config)
- What: Replace `'simple'` with `vi_search` using `unaccent`, stop words, and thesaurus for synonyms (per `Universal_Search.md`).
- Why (UX): Treats diacritics/variants as equivalent, improves recall for regional terms.
- Acceptance:
  - Queries without accents (e.g., "banh mi") match accented content ("bánh mì")
  - Synonym examples (e.g., `thịt heo` ⇔ `thịt lợn`) verified in tests
- Notes: Requires DBA provisioning of tsearch files; migration to switch generated tsvectors to `vi_search`.

7) Diversification of results by type
- What: Cap/round‑robin top results across `restaurant` and `menu_item` to avoid monotony.
- Why (UX): Increases perceived quality and discovery.
- Acceptance: Within top 20, no type exceeds a configured cap; diversity logic is deterministic.

8) Ratings & promos in ranking blend
- What: Incorporate `avg_rating`, `review_count`, and `has_active_promo` into the score (weights via config).
- Why (UX): Elevates quality and deals while keeping relevance.
- Acceptance: Weight knobs behind config; functional tests show expected ordering shifts.

---

### Phase 3 — Personalization & Performance (Medium impact, medium/higher effort)

9) Popular queries and quick entries
- What: Track frequent searches and show localized “quick entries” when term is empty.
- Why (UX): Reduces typing; promotes discovery.
- Acceptance: Empty‑query search returns nearby open restaurants + top cuisines/tags; autocomplete includes popular queries.

10) Geo UX refinements
- What: Add optional radius filters, display distance/ETA ranges; bias ranking by proximity more aggressively when location is present.
- Why (UX): Location‑aware results feel smarter and safer.
- Acceptance: Users can filter within N km; results show distance; ranking adapts to user location.

11) A/B tunable weights and telemetry
- What: Expose ranking weights via configuration; log search sessions, positions, clicks, and conversions for offline analysis.
- Why (UX): Enables fast iteration toward better relevance.
- Acceptance: Weights are runtime‑configurable; basic analytics tables/pipeline exist; privacy reviewed.

---

### Milestone Plan (suggested order and scope)

- Milestone A (2–4 days): Facets + explanations/badges + highlighting
  - API: facet block, badges, snippets
  - Tests: facet correctness, snippet presence, badge rules

- Milestone B (2–4 days): Zero‑results guidance + autocomplete polish
  - API: suggestions block; curated query suggestions; prefix cache
  - Tests: zero‑results behavior; autocomplete latency and ranking

- Milestone C (3–5 days): Vietnamese `vi_search` + diversification + ranking weights
  - Infra: DBA setup; migration to `vi_search`
  - App: diversity pass; rating/promo‑aware scoring with config weights
  - Tests: Vietnamese recall, diversity invariants, ordering under weight changes

- Milestone D (3–5 days): Popular queries + geo refinements + telemetry
  - API: empty‑query discovery; radius filter; distance/ETA fields
  - Data: minimal click logging; config surface for weights

---

### Engineering Notes
- Keep domain pure; all search‑specific logic stays in read‑model and query handlers
- Maintain `source_version` gating for idempotent upserts
- Prefer generated `tsvector` columns; switch to `vi_search` when DBA artifacts are ready
- Autocomplete cache: short TTL (30–60s), safe to invalidate on index changes
- Ensure explainability fields (`Badges`, `Reason`, `DescriptionSnippet`) are optional and bounded in size

### Testing Additions
- Functional tests: facets aggregation, zero‑results suggestions, Vietnamese diacritics/synonyms, diversification, ranking shifts with weights, autocomplete prefix vs trigram
- Load tests: autocomplete under keystroke concurrency; search p95 under common filters

### Success Metrics (initial)
- Search: p95 latency ≤ 300ms (with geo), CTR +5–10% after badges/facets
- Autocomplete: p95 ≤ 120ms, selection rate +10%
- Zero‑results rate reduced by ≥ 20%

---

### TL;DR — Ship in this order for maximum UX lift
1) Facets, badges, highlighting
2) Zero‑results guidance, autocomplete polish (+cache)
3) Vietnamese `vi_search`, diversification, ranking weights (rating/promo)
4) Popular queries, geo refinements, telemetry


