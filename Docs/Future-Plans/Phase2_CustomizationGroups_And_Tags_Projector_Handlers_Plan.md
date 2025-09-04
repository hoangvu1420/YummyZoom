# Phase 2 — Projector Handlers for CustomizationGroups and Tags

Goal: add event-driven projector handlers that keep `FullMenuView` up to date when CustomizationGroup and Tag aggregates change. Follow the existing naive, correct-first rebuild strategy and Application Layer guidelines.

References
- Patterns: see existing handlers in `src/Application/Menus/EventHandlers/*`, `src/Application/MenuCategories/EventHandlers/*`, `src/Application/MenuItems/EventHandlers/*`.
- Read model: `src/Infrastructure/ReadModels/FullMenu/FullMenuViewRebuilder.cs`.
- Guidelines: `Docs/Development-Guidelines/Application_Layer_Guidelines.md` (CQRS, Outbox+Inbox, idempotent handlers).
- Domain events
  - CustomizationGroup: `src/Domain/CustomizationGroupAggregate/Events/*`
  - Tag: `src/Domain/TagEntity/Events/*`

Scope
- Implement event handlers for:
  - CustomizationGroupCreated
  - CustomizationGroupDeleted
  - CustomizationChoiceAdded / Removed / Updated
  - CustomizationChoicesReordered (included for completeness)
  - TagUpdated / TagCategoryChanged / TagDeleted (TagCreated: likely no-op; include safely)
- Strategy: naive rebuild of `FullMenuView` for affected restaurant(s); idempotent via Inbox.

Non-Goals
- No change to rebuild logic shape or JSON contract.
- No optimization beyond selecting the minimal affected restaurant set.

Design Overview
- Create a `CustomizationGroupProjectorBase<TEvent>` mirroring `MenuItemProjectorBase<TEvent>` to encapsulate:
  - Idempotency (`IdempotentNotificationHandler<TEvent>`)
  - `RebuildForRestaurantAsync(Guid restaurantId, CancellationToken)` via `IMenuReadModelRebuilder`
  - `ResolveRestaurantIdAsync(CustomizationGroupId)` via `ICustomizationGroupRepository`
- For Tag events (which do not include `RestaurantId`), use a `TagProjectorBase<TEvent>` that queries impacted restaurants via `IDbConnectionFactory` + Dapper over `MenuItems.DietaryTagIds` (jsonb) and triggers rebuilds via `IMenuReadModelRebuilder`.
- Handlers follow existing patterns: log, resolve restaurant scope, call rebuild, swallow errors (log) to avoid breaking the originating command; rely on outbox retry and inbox idempotency.

Step-by-Step Plan

1) Application: Base for CustomizationGroup projectors
- Add `src/Application/CustomizationGroups/EventHandlers/Shared/CustomizationGroupProjectorBase.cs`:
  - Derive from `IdempotentNotificationHandler<TEvent>`
  - Inject: `IUnitOfWork`, `IInboxStore`, `IMenuReadModelRebuilder`, `ICustomizationGroupRepository`, `ILogger`
  - Helpers:
    - `Task RebuildForRestaurantAsync(Guid restaurantId, CancellationToken ct)` (copy from `MenuItemProjectorBase`)
    - `Task<Guid?> ResolveRestaurantIdAsync(CustomizationGroupId groupId, CancellationToken ct)`
      - Use repo to fetch (including soft-deleted) and return `RestaurantId.Value`
      - If not found, warn and no-op

2) Application: CustomizationGroup event handlers
- Add classes under `src/Application/CustomizationGroups/EventHandlers/`:
  - `CustomizationGroupCreatedEventHandler : CustomizationGroupProjectorBase<CustomizationGroupCreated>`
    - RestaurantId present in event → `RebuildForRestaurantAsync(event.RestaurantId.Value)`
  - `CustomizationGroupDeletedEventHandler : CustomizationGroupProjectorBase<CustomizationGroupDeleted>`
    - Resolve restaurant by group id (must include soft-deleted) → rebuild
  - `CustomizationChoiceAddedEventHandler : CustomizationGroupProjectorBase<CustomizationChoiceAdded>`
    - Resolve restaurant by group id → rebuild
  - `CustomizationChoiceRemovedEventHandler : CustomizationGroupProjectorBase<CustomizationChoiceRemoved>`
    - Resolve restaurant by group id → rebuild
  - `CustomizationChoiceUpdatedEventHandler : CustomizationGroupProjectorBase<CustomizationChoiceUpdated>`
    - Resolve restaurant by group id → rebuild
  - `CustomizationChoicesReorderedEventHandler : CustomizationGroupProjectorBase<CustomizationChoicesReordered>`
    - Resolve restaurant by group id → rebuild

3) Application: Tag projector base for Tag events
- Add `src/Application/Tags/EventHandlers/Shared/TagProjectorBase.cs`:
  - Derive from `IdempotentNotificationHandler<TEvent>`
  - Inject: `IUnitOfWork`, `IInboxStore`, `IMenuReadModelRebuilder`, `IDbConnectionFactory`, `ILogger`
  - Helpers:
    - `Task RebuildForRestaurantAsync(Guid restaurantId, CancellationToken ct)` (same pattern as other bases)
    - `Task<IReadOnlyList<Guid>> FindRestaurantsByTagAsync(TagId tagId, CancellationToken ct)` using Dapper:
      - SQL:
        """
        SELECT DISTINCT mi."RestaurantId"
        FROM "MenuItems" mi
        WHERE mi."IsDeleted" = FALSE
          AND EXISTS (
            SELECT 1
            FROM jsonb_array_elements_text(mi."DietaryTagIds") AS elt(value)
            WHERE elt.value::uuid = @TagId
          );
        """

4) Application: Tag event handlers
- Add `src/Application/Tags/EventHandlers/`:
  - `TagUpdatedEventHandler : TagProjectorBase<TagUpdated>` → find impacted restaurants, rebuild each
  - `TagCategoryChangedEventHandler : TagProjectorBase<TagCategoryChanged>` → same
  - `TagDeletedEventHandler : TagProjectorBase<TagDeleted>` → same (often no-op)
  - `TagCreatedEventHandler : TagProjectorBase<TagCreated>` (optional) → usually no-op

6) Repository enhancements (to enable soft-deleted resolution)
- Extend `ICustomizationGroupRepository` with:
  - `Task<Guid?> GetRestaurantIdByIdIncludingDeletedAsync(CustomizationGroupId id, CancellationToken ct = default);`
- Implement in `CustomizationGroupRepository` using `IncludeSoftDeleted()` and `.Select(g => g.RestaurantId.Value)`.
- Used by CustomizationGroup projector base when events do not carry restaurant id.

7) DI wiring
- No additional DI for tag impact discovery is needed; use existing `IDbConnectionFactory` and `IMenuReadModelRebuilder` registrations.
- `ICustomizationGroupRepository` already registered; just add the new method.

8) Logging & errors
- Mirror existing handlers:
  - Log `Information` at start with EventId and key ids (groupId, tagId, restaurantId).
  - Wrap rebuild in try/catch to log `Error` and swallow (allow outbox retries).

9) Tests (functional) — follow Outbox/Inbox pattern from existing tests
- Add new test classes under `tests/Application.FunctionalTests/Features/CustomizationGroups/Events/` and `.../Tags/Events/`:
  - Arrange: create restaurant/menu/category/items; use commands to create groups/assign to items and to update/delete choices; set dietary tags on items.
  - Act: execute command(s) that emit the target event; `DrainOutboxAsync()` once or twice.
  - Assert:
    - An `InboxMessage` with handler full type name exists exactly once per event (idempotency).
    - `FullMenuView` row for the restaurant exists and `LastRebuiltAt` advances on first drain and remains stable on second.
    - For Tag rename/category change: verify `tagLegend.byId[tagId].name/category` updated in `MenuJson`.
    - For choice changes: verify `customizationGroups.byId[groupId].options` reflect add/remove/update/reorder.
  - Negative case: TagUpdated for a tag unused by any items → no `FullMenuView` mutation (assert unchanged `LastRebuiltAt`).

10) Edge cases and notes
- TagDeleted: items may still list the tag id in `dietaryTagIds`; `tagLegend` should drop the entry since rebuild selects only non-deleted tags. This is acceptable; UI can ignore unknown ids or use legend to render labels.
- CustomizationGroupDeleted: items with assigned group should already be handled by `MenuItemCustomizationRemoved` command/handler paths; still safe to rebuild on group deletion for completeness.
- Handler concurrency: if multiple handlers rebuild the same restaurant in short succession, the last write wins; acceptable for naive stage.

11) Rollout checklist
- [x] Add base + handlers (CustomizationGroups)
- [x] Add impact finder for Tags (implemented in TagProjectorBase via Dapper)
- [x] Add handlers (Tags)
- [x] Extend `ICustomizationGroupRepository` for soft-deleted id resolution
- [x] Register impact finder in DI (not required separately; uses existing `IDbConnectionFactory`)
- [x] Add functional tests for CustomizationGroup handlers (idempotency + JSON assertions)
- [x] Add functional tests for Tag handlers (idempotency + JSON assertions)
- [ ] Smoke test in local environment: run write flows and observe `FullMenuView` updates

Implementation Details (Naming & Paths)
- Base classes
  - `src/Application/CustomizationGroups/EventHandlers/Shared/CustomizationGroupProjectorBase.cs`
  - `src/Application/Tags/EventHandlers/Shared/TagProjectorBase.cs`
- CustomizationGroup handlers
  - `src/Application/CustomizationGroups/EventHandlers/CustomizationGroupCreatedEventHandler.cs`
  - `src/Application/CustomizationGroups/EventHandlers/CustomizationGroupDeletedEventHandler.cs`
  - `src/Application/CustomizationGroups/EventHandlers/CustomizationChoiceAddedEventHandler.cs`
  - `src/Application/CustomizationGroups/EventHandlers/CustomizationChoiceRemovedEventHandler.cs`
  - `src/Application/CustomizationGroups/EventHandlers/CustomizationChoiceUpdatedEventHandler.cs`
  - `src/Application/CustomizationGroups/EventHandlers/CustomizationChoicesReorderedEventHandler.cs`
 
- Tag handlers
  - `src/Application/Tags/EventHandlers/TagUpdatedEventHandler.cs`
  - `src/Application/Tags/EventHandlers/TagCategoryChangedEventHandler.cs`
  - `src/Application/Tags/EventHandlers/TagDeletedEventHandler.cs`
  - Optional: `src/Application/Tags/EventHandlers/TagCreatedEventHandler.cs`

Alignment with Application Layer Guidelines
- CQRS: handlers trigger read-model rebuilds (query-shaped work) via services; no aggregate mutations inside handlers.
- Outbox/Inbox: derive from `IdempotentNotificationHandler<TEvent>`; do work inside unit-of-work and add inbox record.
- Dapper for read-side utility: TagProjectorBase uses `IDbConnectionFactory` and direct SQL for impact discovery.
- Testing: functional tests drain outbox and assert inbox entries + read-model changes.

Estimated Effort
- Handlers + bases (CustomizationGroup + Tag): 1–1.25 days
- Repository enhancement: 0.25 day
- Tests (6–8 scenarios): 1–1.5 days
- Total: ~2.5–3 days including reviews and polish

Risks & Mitigations
- Risk: Tag handlers accidentally rebuild all restaurants. Mitigation: targeted Dapper SQL in TagProjectorBase to select only impacted restaurants.
- Risk: Failing to resolve restaurant for deleted groups. Mitigation: repository method including soft-deleted.
- Risk: Multiple rapid rebuilds thrash. Mitigation: acceptable at this stage; consider coalescing later if needed.
