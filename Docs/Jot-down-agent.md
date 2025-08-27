Refined functional test plan for RebuildFullMenu (aligned with Application-Functional-Tests-Guidelines)

### Where the tests live
- File: `tests/Application.FunctionalTests/Features/Admin/RebuildFullMenuCommandTests.cs`
- Use the unified facade in `Testing.cs` (SendAsync, FindAsync, DrainOutboxAsync, RunAs... helpers) and centralized data from `TestDataFactory`/`DefaultTestData`.

### Principles to follow (from the guidelines)
- Real Postgres via Testcontainers; per-test DB reset with Respawner.
- Arrange using the provided factories; avoid ad-hoc seeding in tests.
- Act via `SendAsync(command)`; drain outbox only if asserting side-effects driven by events (the rebuilder runs synchronously in the command handler, so typically no drain is needed here).
- Assert persisted state via direct `FindAsync<TEntity>()` calls and content checks against `FullMenuViews`.

### Test cases
- Rebuild_PopulatesFullMenuView_AndIsIdempotent (kept)
  - Assert initial creation and equality of `MenuJson` across two consecutive runs; `LastRebuiltAt` non-decreasing.

- EnabledMenuRequired_ThrowsWhenNoEnabledMenu
  - Arrange: restaurant without an enabled menu (disable default or create a fresh restaurant without menus using factory).
  - Act: `SendAsync`.
  - Assert: failure (InvalidOperationException surfaced or mapped); no row in `FullMenuViews`.

- ExcludesSoftDeletedEntities
  - Arrange: default restaurant with enabled menu; create categories/items/groups/tags, then soft-delete a subset via factory helpers.
  - Act: rebuild.
  - Assert: `MenuJson` excludes soft-deleted entities; only active ones appear.

- Categories_AreOrderedByDisplayOrderThenName
  - Arrange: 3+ categories with varying `DisplayOrder` and overlapping names.
  - Act: rebuild.
  - Assert: `categories.order` matches order-by rules; `displayOrder` values preserved.

- Items_UnderCorrectCategory_AndItemOrderByName
  - Arrange: items across categories with names ensuring sort differences.
  - Act: rebuild.
  - Assert: `categories.byId[catId].itemOrder` sorted by item `Name`; `items.byId[*].categoryId` correct.

- Item_Shapes_Price_Availability_Image_Description
  - Arrange: item with base price, availability, optional image.
  - Act: rebuild.
  - Assert: `price.amount/currency`, `isAvailable`, `imageUrl`, `description` match.

- DietaryTags_LegendAndItemTagIds
  - Arrange: attach several `TagId`s to items; include an extra tag not referenced by any item.
  - Act: rebuild.
  - Assert: item `dietaryTagIds` preserved; legend contains only referenced tags with correct `name` and `category` (string enum).

- CustomizationGroups_Composition_AndOrdering
  - Arrange: items with `AppliedCustomizations` referencing multiple groups with differing display orders; groups have choices with display order and names.
  - Act: rebuild.
  - Assert: `customizationGroups.byId[group].min/max/name` correct; `options` ordered by display order then name; each item’s `customizationGroups` sorted by `DisplayOrder`.

- Handles_Empty_Collections
  - Arrange: enabled menu with no categories; categories with no items; items without tags/customizations.
  - Act: rebuild.
  - Assert: empty arrays/maps serialized as empty structures; JSON remains valid.

- Rebuild_UpdatesExistingRow_WhenDataChanges
  - Arrange: run once; change category or item name via factory; run again.
  - Assert: `MenuJson` reflects change; `LastRebuiltAt` advanced.

- MultipleMenus_EnabledPreference
  - Arrange: two menus (one enabled, one disabled) for same restaurant.
  - Act: rebuild.
  - Assert: JSON reflects the enabled menu (ensure deterministic seed in factory to avoid ambiguity).

- LargeDataset_Smoke
  - Arrange: generate many categories/items/groups/tags (e.g., 30/150/20/25) via factory.
  - Act: rebuild.
  - Assert: completes within a reasonable time budget and structures are consistent (counts, shapes). Not a perf benchmark, just a guard.

### JSON assertion strategy
- Parse `MenuJson` with System.Text.Json into lightweight internal DTOs defined in the test file, shaped to the serialized structure:
  - Root: version, restaurantId, menuId, menuName, menuDescription, menuEnabled, lastRebuiltAt, currency.
  - categories: order[], byId{ id -> { id, name, displayOrder, itemOrder[] } }.
  - items: byId{ id -> { id, categoryId, name, description, price{amount,currency}, imageUrl, isAvailable, dietaryTagIds[], customizationGroups[] } }.
  - customizationGroups: byId{ id -> { id, name, min, max, options[] } }.
  - tagLegend: byId{ id -> { name, category } }.
- Assert ordering with FluentAssertions `BeInAscendingOrder` on the materialized lists; assert sets with `BeEquivalentTo` when order is not required.

### New Menu test data factory (options/fluent pattern)
- Introduce `tests/Application.FunctionalTests/TestData/MenuTestDataFactory.cs`, mirroring `CouponTestDataFactory` with an options/fluent builder pattern.
- Options type: `MenuScenarioOptions` to declaratively build menu-related data:
  - `bool EnabledMenu` (default true)
  - `int CategoryCount`, `Func<int, (name, order)> CategoryGenerator`
  - `Func<Guid categoryId, int index, ItemOptions> ItemGenerator`
  - `bool IncludeTags`, `Func<int, TagOptions>`
  - `bool IncludeCustomizationGroups`, `Func<int, GroupOptions>` and linkage from items via `AppliedCustomizations`
  - Flags to soft-delete: categories/items/groups/tags by index selectors

- Factory API (example):
  - `Task<MenuScenarioResult> CreateRestaurantWithMenuAsync(MenuScenarioOptions options)` → returns ids/maps: `RestaurantId`, `MenuId`, `CategoryIds[]`, `ItemIds[]`, `GroupIds[]`, `TagIds[]`.
  - `Task DisableMenuAsync(Guid menuId)` / `Task EnableMenuAsync(Guid menuId)`.
  - `Task SoftDeleteCategoryAsync(Guid categoryId)` / `Task SoftDeleteItemAsync(Guid itemId)` / `Task SoftDeleteGroupAsync(Guid groupId)` / `Task SoftDeleteTagAsync(Guid tagId)`.
  - `Task RenameCategoryAsync(Guid categoryId, string newName)` / `Task RenameItemAsync(Guid itemId, string newName)`.
  - `Task AttachTagsToItemAsync(Guid itemId, IReadOnlyList<Guid> tagIds)` (updates `DietaryTagIds`).
  - `Task AttachCustomizationsToItemAsync(Guid itemId, IReadOnlyList<(Guid groupId, string title, int order)> customizations)` (updates `AppliedCustomizations`).

- Convenience accessors for assertions:
  - `GetFullMenuViewAsync(Guid restaurantId)` to fetch `FullMenuViews` row for direct assertions.

### Notes
- Keep tests small and focused per the guidelines; prefer one behavior per test.
- Use `SendAsync(new RebuildFullMenuCommand { RestaurantId = ... })` for the act step; no need to drain outbox.
- Reuse `DefaultTestData` where sensible; only diverge via factory when specific shapes (soft-deletes, ordering, references) are needed.