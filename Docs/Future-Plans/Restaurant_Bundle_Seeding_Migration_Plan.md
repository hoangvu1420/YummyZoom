# Restaurant Bundle Seeding — Migration Plan

This plan migrates the current restaurant-related seeding to a single-file-per-restaurant bundle, keeping the existing seeding infrastructure (ISeeder, SeedingOrchestrator, SeedingConfiguration) and the run-at-app-start behavior. It focuses only on restaurant catalog data: restaurant → menu → categories → items → customization groups → tags.

References: `Docs/Architecture/Domain_Design.md`, `Docs/Architecture/YummyZoom_Project_Documentation.md`, `Docs/Future-Plans/Data_Seeding_Solution.md`.

## Principles

- Idempotent upserts guided by stable lookups; no duplicate rows.
- Configuration-driven behavior; safe defaults; profile-aware.
- Respect domain invariants and Clean Architecture boundaries.
- Simple authoring: one JSON file per restaurant, human-comprehensible.
- Backward compatible during transition; reversible and low risk.

## Target Outcome

- One bundle file per restaurant at `src/Infrastructure/Persistence/EfCore/Seeding/Data/Restaurants/<restaurant-slug>.restaurant.json` drives creation of: Tags (optional), Restaurant, Menu, MenuCategories, CustomizationGroups, MenuItems, and relationships.
- A new `RestaurantBundleSeeder` processes each file in one transaction per restaurant.
- Existing seeders remain available but disabled by default via configuration; can be removed after verification.

## Bundle Template (High-Level)

- `restaurantSlug` (stable string key for lookup)
- `name`, `description`, `cuisineType`, `logoUrl`, `backgroundImageUrl`
- `address`, `contact`, `businessHours`, `isVerified`, `isAcceptingOrders`
- `tags[]` (optional): `{ tagName, tagCategory, tagDescription? }`
- `customizationGroups[]` (optional): `{ groupKey, minSelections, maxSelections, choices[]: { name, priceAdjustment, isDefault?, displayOrder? } }`
- `menu`: `{ name, description, categories[] }`
- `categories[]`: `{ name, displayOrder, items[] }`
- `items[]`: `{ name, description, basePrice, imageUrl?, isAvailable, dietaryTags[]?, customizationGroups[]? }`

Lookup keys during upsert:
- Restaurant by `restaurantSlug` (primary), falling back to case-insensitive `name` only for initial migration.
- Menu by `(restaurantId, menu.name)`.
- Category by `(menuId, category.name)`.
- CustomizationGroup by `(restaurantId, groupKey)`.
- MenuItem by `(menuCategoryId, item.name)`.

Note: Name-based identity keeps authoring simple. An optional `itemKey`/`categoryKey` can be introduced later to support safe renames.

## Phases

### 0) Alignment & Readiness
- Review domain relationships in `Docs/Architecture/Domain_Design.md`:
  - Restaurant (aggregate root)
  - Menu, MenuCategory (independent entities linked by IDs)
  - MenuItem (aggregate), linked to Restaurant and Category
  - CustomizationGroup (aggregate) scoped to Restaurant; AppliedCustomization on MenuItem
  - Tag (entity) + TagCategory enum (dietary, cuisine, spice level)
- Acceptance Criteria:
  - Mapping table prepared for bundle fields → domain factories/VOs.
  - Confirm transaction boundaries: one transaction per restaurant file.
  - Confirm run-at-start orchestration remains unchanged.

Implementation Notes:
- Keep `SeedingOrchestrator` ordering; place `RestaurantBundleSeeder` after Tag seeding if Tags remain global, or let the bundle seeder upsert needed tags early.

### 1) Configuration & Feature Flag
- Add `SeedingConfiguration.SeederSettings["RestaurantBundle"]` options:
  - `ReportOnly` (bool, default false): validate + log actions, no DB writes.
  - `UpdateDescriptions` (bool, default false).
  - `UpdateBasePrices` (bool, default false).
  - `RestaurantGlobs` (string[], optional): filter which bundle files to load.
- Acceptance Criteria:
  - App reads settings from `appsettings.*.json` under `Seeding`.
  - Existing seeders can be disabled via `EnabledSeeders` without code removal.

Implementation Notes:
- Keep default behavior safe (create-only). Enable updates explicitly for staging.

### 2) Bundle DTOs & Validation
- Introduce DTOs that mirror the “Bundle Template (High-Level)”.
- Add a minimal validator (required fields, numeric ranges, allowed categories for tags).
- Acceptance Criteria:
  - Invalid files abort seeding for that restaurant with clear logs; other restaurants continue.
  - Validation logs list missing/invalid fields and file path.

Implementation Notes:
- Keep JSON simple; JSON Schema can be added later. Use `JsonSerializer` with case-insensitive properties.

### 3) New Seeder
- Add `RestaurantBundleSeeder : ISeeder` with order ~100–140.
- Logic per file (one transaction):
  - Parse + validate.
  - Upsert Tags in `tags[]` (if provided).
  - Upsert Restaurant by `restaurantSlug`.
  - Upsert Menu by `(restaurantId, menu.name)`.
  - Upsert Categories by `(menuId, category.name)`.
  - Upsert CustomizationGroups by `(restaurantId, groupKey)`; add choices.
  - Upsert MenuItems by `(menuCategoryId, item.name)`; set dietary tags and attach customization groups.
  - Clear domain events for inserted aggregates; SaveChanges once at end.
- Acceptance Criteria:
  - Seeding is idempotent (second run: zero creates; updates only if enabled in settings).
  - Logs show created/updated/skipped counts per file and entity type.

Implementation Notes:
- Reuse current domain factory methods (e.g., `Restaurant.Create`, `Menu.Create`, `MenuItem.Create`).
- Use `AsNoTracking` for reads and build in-memory lookup maps per file for performance.

### 4) File Layout & Discovery
- Create folder `src/Infrastructure/Persistence/EfCore/Seeding/Data/Restaurants/`.
- File naming: `<restaurant-slug>.restaurant.json` (e.g., `bella-vista.restaurant.json`).
- Discovery: the seeder enumerates `*.restaurant.json` (honors `RestaurantGlobs` if set).
- Acceptance Criteria:
  - Developer can add/modify a single file to extend data for a restaurant.
  - No changes required across multiple template files.

Implementation Notes:
- Keep existing `Templates` folder for backward compatibility during migration.

### 5) Migration of Existing Templates
- Source files:
  - `Templates/RestaurantTemplates.json`
  - `Templates/MenuItemTemplates.json` (categories + items)
  - `Templates/CustomizationGroupTemplates.json`
  - `Templates/TagsTemplates.json`
- Steps:
  - Generate one bundle per restaurant entry from `RestaurantTemplates.json`.
  - Copy categories and items from `MenuItemTemplates.json` into each bundle’s `menu.categories[]` (initially identical across restaurants; can diverge later).
  - Copy customization groups into each bundle (or keep centralized and paste as needed).
  - Optionally copy globally defined tags used by items into each bundle’s optional `tags[]`.
- Acceptance Criteria:
  - For each restaurant present today, a corresponding bundle file exists and passes validation.
  - Seeding from bundles yields the same or strictly superset data as before.

Implementation Notes:
- Write a small one-off internal tool (or script) to transform current templates into per-restaurant bundles to reduce manual work. Keep tool outside production code or behind `#if DEBUG`.

### 6) Idempotency, Updates, and Policies
- Default behavior: create-if-missing; skip on existence.
- Optional updates controlled by `SeedingConfiguration.SeederSettings["RestaurantBundle"]`:
  - `UpdateDescriptions`: overwrite text fields when template differs.
  - `UpdateBasePrices`: overwrite price fields.
- Acceptance Criteria:
  - With all update flags false, rerun produces no changes.
  - With `UpdateDescriptions` true, changes apply only to allowed fields.

Implementation Notes:
- Price and text comparison should be tolerant of whitespace and minor formatting differences.

### 7) Logging, Reporting, and Dry Run
- Add concise logs per restaurant file: `Created/Updated/Skipped` per entity type and a summary line.
- `ReportOnly` outputs the same summary without DB writes.
- Acceptance Criteria:
  - A developer can preview the impact of changes by toggling `ReportOnly`.
  - Errors are actionable and show file + entity context.

Implementation Notes:
- Reuse existing `ILogger` instances and the orchestrator’s start/finish markers.

### 8) Performance & Scale (Staging Simulation)
- Preload lookups: tags by name (Dietary), groups by key, categories by name per menu.
- Batch inserts by entity type within the transaction; single `SaveChanges` at the end.
- Acceptance Criteria:
  - Able to seed dozens of restaurants with hundreds of items in acceptable startup time.
  - No N+1 hotspots in profiling of the seeder paths.

Implementation Notes:
- Keep `AsNoTracking` read paths (already used in current seeders). Avoid repeated per-item database calls.

### 9) Cutover & Cleanup
- During migration window:
  - Disable existing `Restaurant`, `Menu`, `MenuCategory`, `CustomizationGroup`, and `MenuItem` seeders via `EnabledSeeders` when bundle seeder is enabled.
  - Keep `TagSeeder` enabled for global tags not present in bundles (or let bundle seeder upsert per-file tags first).
- After verification:
  - Remove old template files and the redundant seeders.
- Acceptance Criteria:
  - One-pass seeding uses only bundle files for restaurant data.
  - CI/Smoke runs green using only the new seeder.

Implementation Notes:
- Stage the removal in a dedicated PR after a full dry-run and one real run in a dev/staging environment.

## Acceptance Checklist

- One bundle file per restaurant exists and validates.
- `RestaurantBundleSeeder` runs at app start, processes files, and logs summaries.
- Idempotent behavior verified (two consecutive runs → no new rows; updates apply only when enabled).
- Data parity with the old approach verified for the current sample restaurants.
- Performance acceptable for staging-volume data.

## Developer Workflow

- Add a new restaurant: create `Data/Restaurants/<slug>.restaurant.json` with full graph; run app.
- Modify categories/items or groups: edit the same file; run with `ReportOnly=true` to preview and then apply.
- Control updates via `Seeding.SeederSettings.RestaurantBundle` in `appsettings.*.json`.

## Risks & Mitigations

- Name-based identity can hinder safe renames → introduce optional `itemKey/categoryKey` later if needed.
- Large files become unwieldy → split by cuisine folders or introduce `$include` in a future iteration (out of scope for now).
- Divergence between global tags and per-file tags → keep `TagSeeder` enabled as a fallback.

## Implementation Pointers (Code-Level)

- Use existing factories and value objects:
  - Restaurant: `Restaurant.Create(...)` then `Verify()` / `AcceptOrders()` as needed.
  - Menu: `Menu.Create(restaurantId, name, description, isEnabled: true)`.
  - Category: `MenuCategory.Create(menu.Id, category.Name, category.DisplayOrder)`.
  - CustomizationGroup: `CustomizationGroup.Create(restaurantId, groupKey, min, max)` then `AddChoice(...)`.
  - MenuItem: `MenuItem.Create(restaurantId, categoryId, name, description, Money(basePrice, "USD"), imageUrl, isAvailable)`.
  - Dietary tags: resolve IDs by name; attach to MenuItem if found.
  - Customization assignment: resolve group by `groupKey`, then `AssignCustomizationGroup(AppliedCustomization.Create(...))` in a stable order.

- Ordering in orchestrator:
  - Keep `TagSeeder` early (Order ≈105) or let bundle seeder ensure tags exist per-file first.
  - Place `RestaurantBundleSeeder` Order ≈110–140.

## Example appsettings (Staging)

```json
{
  "Seeding": {
    "Profile": "StagingSim",
    "EnableIdempotentSeeding": true,
    "SeedTestData": true,
    "EnabledSeeders": {
      "Restaurant": false,
      "Menu": false,
      "MenuCategory": false,
      "CustomizationGroup": false,
      "MenuItem": false,
      "Tag": true,
      "RestaurantBundle": true
    },
    "SeederSettings": {
      "RestaurantBundle": {
        "ReportOnly": false,
        "UpdateDescriptions": true,
        "UpdateBasePrices": false,
        "RestaurantGlobs": ["*.restaurant.json"]
      }
    }
  }
}
```

## Done When

- The old per-entity templates are no longer needed for restaurant data.
- Developers can fully define or extend a restaurant’s catalog using a single bundle file.
- Seeding remains idempotent, configurable, performant, and aligned with domain rules documented in `Docs/Architecture/Domain_Design.md`.

## Implementation Status (This PR)

- Phase 0 (Alignment): Completed; domain relationships verified.
- Phase 1 (Config/Flag): Completed; typed options + extension method.
- Phase 2 (DTOs/Validation): Completed; bundle DTOs and validator added.
- Phase 3 (New Seeder): Completed; `RestaurantBundleSeeder` implemented and registered.
- Phase 4 (File Discovery): Completed; csproj includes embedded `*.restaurant.json` resources.
- Phase 5 (Migrate Existing Templates): Completed for sample data; added 3 bundle files:
  - `src/Infrastructure/Persistence/EfCore/Seeding/Data/Restaurants/bella-vista-italian.restaurant.json`
  - `src/Infrastructure/Persistence/EfCore/Seeding/Data/Restaurants/sakura-sushi.restaurant.json`
  - `src/Infrastructure/Persistence/EfCore/Seeding/Data/Restaurants/el-camino-taqueria.restaurant.json`
  These are derived from the existing Restaurant/MenuItem/CustomizationGroup templates.
- Phase 6 (Idempotency/Updates): Implemented via create-if-missing and optional Update* flags.
- Phase 7 (Logging/Dry Run): Implemented; per-file summary + `ReportOnly` mode.
- Phase 8 (Performance): Implemented; single SaveChanges per file; preloaded lookups and AsNoTracking reads.
- Phase 9 (Cutover): Old per-entity seeders and templates removed; DI now registers only `TagSeeder` and `RestaurantBundleSeeder`. Templates kept: `TagsTemplates.json` (global dietary/cuisine tags). New bundle files embedded and used by default.
