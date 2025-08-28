
## Receipt and Plan

I’ve reviewed the current Phase 2, Step 2 outline and related design docs, located the read-model rebuilder and domain events, and studied our established Application functional test patterns via the Orders event handler tests. Below is a comprehensive, actionable implementation plan for Phase C: MenuItem event-driven projector.

## Deep Analysis of Current State

- Current plan (Docs/Future-Plans/Phase2_Step2_Implementation_Outline.md)
  - Phase C prescribes a naive, correct-first approach: rebuild the entire restaurant’s FullMenuView on any MenuItem event.
  - Calls for implementing a small set of IdempotentNotificationHandlers for MenuItem events and sharing rebuild logic.

- Design context (Docs/Feature-Discover/5-MenuItem.md)
  - MenuItem commands and events defined for: create, update details, price changes, availability changes, move to category, update tags, customization assignments, deletion.
  - Reactors to consider beyond FullMenuView updates: search index updates, coupon process manager for delete, etc. Those can be separate handlers and phased later.

- Read-model and infrastructure (existing)
  - IMenuReadModelRebuilder interface and FullMenuViewRebuilder concrete implementation already exist and are used by the admin RebuildFullMenu command.
  - FullMenuView schema stores a single JSON document per restaurant with lastRebuiltAt.

````csharp path=src/Application/Restaurants/Queries/Common/IMenuReadModelRebuilder.cs mode=EXCERPT
  public interface IMenuReadModelRebuilder
  {
      Task<(string menuJson, DateTimeOffset lastRebuiltAt)> RebuildAsync(Guid restaurantId, CancellationToken ct = default);
      Task UpsertAsync(Guid restaurantId, string menuJson, DateTimeOffset lastRebuiltAt, CancellationToken ct = default);
  }
````

````csharp path=src/Infrastructure/ReadModels/FullMenu/FullMenuViewRebuilder.cs mode=EXCERPT
  public async Task UpsertAsync(Guid restaurantId, string menuJson, DateTimeOffset lastRebuiltAt, CancellationToken ct = default)
  {
      const string sql = /* upsert FullMenuViews */;
      await connection.ExecuteAsync(new CommandDefinition(sql, new { RestaurantId = restaurantId, MenuJson = menuJson, LastRebuiltAt = lastRebuiltAt }, cancellationToken: ct));
  }
````

- Idempotency pattern for event handlers
  - Handlers derive from IdempotentNotificationHandler<T>, which guards via Inbox messages and wraps work inside a transaction.

````csharp path=src/Application/Common/Notifications/IdempotentNotificationHandler.cs mode=EXCERPT
  public async Task Handle(TEvent notification, CancellationToken ct)
  {
      var handlerName = GetType().FullName!;
      var already = await _inbox.ExistsAsync(notification.EventId, handlerName, ct);
      if (already) return;
      await _uow.ExecuteInTransactionAsync(async () => { /* HandleCore + write inbox */ }, ct);
  }
````

- Domain events available (src/Domain/MenuItemAggregate/Events)
  - Present: MenuItemCreated, MenuItemAvailabilityChanged, MenuItemDetailsUpdated, MenuItemPriceChanged, MenuItemDietaryTagsUpdated, MenuItemCustomizationAssigned, MenuItemCustomizationRemoved, MenuItemAssignedToCategory, MenuItemDeleted.
  - Note: Most events do NOT include RestaurantId (except Created). Handlers will need to resolve RestaurantId from MenuItemId.

- Current gaps
  - No Application MenuItems/EventHandlers exist yet (directory present with Commands only).
  - No functional tests for MenuItem event handlers yet; we will mirror the Orders event handler test pattern.
  - Soft delete is enabled globally; this impacts how Deleted event resolves RestaurantId (must query including soft-deleted entities).

## Event Handlers: Responsibilities and Side Effects

All handlers share base responsibilities:
- Resolve affected restaurant for the event.
- Rebuild FullMenuView for that restaurant via IMenuReadModelRebuilder.RebuildAsync + UpsertAsync.
- Ensure idempotency via IdempotentNotificationHandler.
- Log success/failures; do not fail the originating command on projector errors.
- Structure code for future optimization (partial updates).

Beyond FullMenuView rebuilds, recommended/optional side-effects per event (to schedule as separate handlers or next phase):

- MenuItemCreated
  - Primary: ensure new item appears in FullMenuView.
  - Optional: add/update restaurant search index entry for the item.

- MenuItemAvailabilityChanged
  - Primary: ensure isAvailable reflects correctly; affects order flows UI.
  - Optional: invalidate any cached management read (e.g., GetMenuItemsForManagement) if caching exists.

- MenuItemDetailsUpdated
  - Primary: name/description/image updates reflected.
  - Optional: update search index if item name changes; invalidate CDN image if you have such a pipeline (future).

- MenuItemPriceChanged
  - Primary: update price JSON.
  - Optional: write to MenuItemPriceHistory audit table (future); flag open carts recalculation (future design).

- MenuItemDietaryTagsUpdated
  - Primary: tags reflected; tag legend updates via rebuild.
  - Optional: adjust faceted search index for tags.

- MenuItemCustomizationAssigned / MenuItemCustomizationRemoved
  - Primary: reflect customization group presence/absence and ordering in JSON.
  - Cascading: selection limits and option lists must reflect accurately for customer app.

- MenuItemAssignedToCategory
  - Primary: item moves to the new category; item ordering within the new category is alphabetical (per current FullMenuView composition).
  - Optional: update management read caches.

- MenuItemDeleted
  - Primary: remove item from FullMenuView.
  - Important: handler must resolve RestaurantId from a soft-deleted row; see Dependencies.

## Dependencies and Integration Points

- IMenuReadModelRebuilder (existing)
- IdempotentNotificationHandler<T> (existing)
- IUnitOfWork, IInboxStore (existing)
- Logging (ILogger<T>)
- Menu item repository method to resolve RestaurantId by MenuItemId including soft-deleted rows
  - Proposed: IMenuItemRepository.GetRestaurantIdByIdIncludingDeletedAsync(MenuItemId, ct) returning RestaurantId? or Guid?
  - Infrastructure: in MenuItemRepository, implement using IncludeSoftDeleted() and a projection to only RestaurantId for performance.
- Soft delete awareness (IncludeSoftDeleted) for Deleted event and resilience in other handlers.
- Tests: Testing.DrainOutboxAsync(), ApplicationDbContext access to FullMenuViews table.

## Implementation Design

### 1) Shared Base: MenuItemProjectorBase

- Inherits IdempotentNotificationHandler<TEvent>.
- Provides protected helper RebuildForRestaurant(Guid restaurantId, ct):
  - Calls _rebuilder.RebuildAsync(restaurantId, ct)
  - Calls _rebuilder.UpsertAsync(restaurantId, menuJson, rebuiltAt, ct)
  - Catches/logs exceptions; rethrows only if needed (normally log and return).

- Provides protected helper ResolveRestaurantIdAsync(MenuItemId id, ct):
  - Uses repository method that includes soft-deleted entities.
  - If not found, log warning and return null to no-op; inbox entry remains to mark idempotent handling.

Skeleton (new code example):
```csharp
public abstract class MenuItemProjectorBase<TEvent> : IdempotentNotificationHandler<TEvent>
    where TEvent : IDomainEvent, IHasEventId
{
    protected readonly IMenuReadModelRebuilder _rebuilder;
    protected readonly IMenuItemRepository _menuItemRepository; // with "include deleted" helper
    protected readonly ILogger _logger;

    protected async Task RebuildForRestaurant(Guid restaurantId, CancellationToken ct)
    {
        var (json, rebuiltAt) = await _rebuilder.RebuildAsync(restaurantId, ct);
        await _rebuilder.UpsertAsync(restaurantId, json, rebuiltAt, ct);
    }

    protected async Task<Guid?> ResolveRestaurantIdAsync(MenuItemId itemId, CancellationToken ct)
    {
        return await _menuItemRepository.GetRestaurantIdByIdIncludingDeletedAsync(itemId, ct);
    }
}
```

### 2) Handlers and RestaurantId Resolution Rules

- MenuItemCreatedEventHandler:
  - RestaurantId available on event; call RebuildForRestaurant(event.RestaurantId.Value).

- All others (AvailabilityChanged, DetailsUpdated, PriceChanged, DietaryTagsUpdated, CustomizationAssigned/Removed, AssignedToCategory, Deleted):
  - Resolve restaurant via ResolveRestaurantIdAsync(event.MenuItemId).
  - If null, log warning and return (no-op projector).

### 3) Error Handling and Idempotency

- Rely on IdempotentNotificationHandler to:
  - Check inbox before work.
  - Wrap in transaction.
  - Insert inbox message after successful HandleCore.
- Log with eventId, menuItemId, restaurantId when available.
- Do not throw up to let outbox mark “processed” only when HandleCore completes; otherwise, outbox retry policy will reattempt.

## Specific Implementation Steps (per handler)

For each handler below, the general steps are the same; only the RestaurantId source differs:

- Inject: IUnitOfWork, IInboxStore, IMenuReadModelRebuilder, IMenuItemRepository (for non-Created), ILogger<T>.
- HandleCore:
  1. Determine restaurantId (directly or via repo).
  2. If unavailable, log warning and return.
  3. await RebuildForRestaurant(restaurantId, ct).

Handlers:
- MenuItemCreatedEventHandler
- MenuItemAvailabilityChangedEventHandler
- MenuItemDetailsUpdatedEventHandler
- MenuItemPriceChangedEventHandler
- MenuItemDietaryTagsUpdatedEventHandler
- MenuItemCustomizationAssignedEventHandler
- MenuItemCustomizationRemovedEventHandler
- MenuItemAssignedToCategoryEventHandler
- MenuItemDeletedEventHandler (requires include-deleted resolution)

## Functional Test Strategy and Specs

Pattern to mirror from Orders event handler tests:
- Arrange: Execute a command that triggers a domain event.
- Act: await DrainOutboxAsync() one or two times.
- Assert:
  - The FullMenuViews row exists and its MenuJson reflects the expected change.
  - Inbox contains a single entry for the handler (idempotency).
  - Outbox messages for the event are processed, no errors.
  - Optionally assert lastRebuiltAt changed.

Common helpers:
- Use MenuTestDataFactory for composing scenarios.
- Use Testing.DrainOutboxAsync().
- Query FullMenuViews via ApplicationDbContext and parse MenuJson (JsonDocument) to assert fields.

Proposed test files (one per event) under tests/Application.FunctionalTests/Features/MenuItems/Events/:

1) MenuItemCreatedEventHandlerTests
- CreateMenuItemCommand creates a new item.
- Drain outbox.
- Assert FullMenuView includes the new item with correct fields.
- Assert single InboxMessage for handler, Outbox processed without errors.

2) MenuItemAvailabilityChangedEventHandlerTests
- Toggle availability via ChangeMenuItemAvailabilityCommand.
- Drain outbox twice (idempotency).
- Assert isAvailable updated in FullMenuView.
- Assert single inbox entry and processed outbox.

3) MenuItemDetailsUpdatedEventHandlerTests
- Update details via UpdateMenuItemDetailsCommand.
- Drain outbox.
- Assert name/description/imageUrl in FullMenuView.
- Idempotency assertions.

4) MenuItemPriceChangedEventHandlerTests
- Update price (if price command exists; else updating details includes price per plan).
- Drain outbox.
- Assert price.amount and currency updated in FullMenuView.
- Idempotency assertions.

5) MenuItemDietaryTagsUpdatedEventHandlerTests
- Update tags via UpdateMenuItemDietaryTagsCommand.
- Drain outbox.
- Assert item’s dietaryTagIds reflect new list.
- Idempotency assertions.

6) MenuItemCustomizationAssigned/RemovedEventHandlerTests
- Assign then remove customization via corresponding commands.
- Drain outbox after each.
- Assert customizationGroups list includes/excludes group with correct ordering.
- Idempotency assertions.

7) MenuItemAssignedToCategoryEventHandlerTests
- Assign item to a different category.
- Drain outbox.
- Assert item’s categoryId is new one; category ordering of the item list is alphabetical.
- Idempotency assertions.

8) MenuItemDeletedEventHandlerTests
- DeleteMenuItemCommand soft-deletes the item.
- Drain outbox.
- Assert FullMenuView no longer contains the item.
- Assert handler could resolve RestaurantId despite soft delete (implicitly validated by success of rebuild).
- Idempotency assertions.

Verification details to copy from existing patterns:
- Inbox check:
````csharp path=tests/Application.FunctionalTests/Features/Orders/Events/OrderPlacedEventHandlerTests.cs mode=EXCERPT
  var handlerName = typeof(OrderPlacedEventHandler).FullName!;
  var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
  inboxEntries.Should().HaveCount(1);
````
- Outbox processed check:
````csharp path=tests/Application.FunctionalTests/Features/Orders/Events/OrderAcceptedEventHandlerTests.cs mode=EXCERPT
  var processed = await db.Set<OutboxMessage>().Where(m => m.Type.Contains("OrderAccepted")).ToListAsync();
  processed.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
````

## Implementation Sequence and Dependencies

Recommended sequence to minimize risk and rework:

1) Add repository method to resolve RestaurantId including soft-deleted entities:
   - IMenuItemRepository: GetRestaurantIdByIdIncludingDeletedAsync(MenuItemId, ct)
   - Implement in MenuItemRepository using IncludeSoftDeleted() and Select(m => m.RestaurantId.Value) for efficiency.

2) Create MenuItemProjectorBase<TEvent> in Application/MenuItems/EventHandlers/Shared/:
   - Inherit IdempotentNotificationHandler<TEvent>
   - Implement RebuildForRestaurant(...) and ResolveRestaurantIdAsync(...).

3) Implement the simplest handler first (MenuItemCreatedEventHandler)
   - Use event.RestaurantId directly.
   - Add functional tests for Created.

4) Implement AvailabilityChanged and DetailsUpdated handlers
   - Add functional tests.

5) Implement PriceChanged, DietaryTagsUpdated handlers
   - Add functional tests.

6) Implement CustomizationAssigned and CustomizationRemoved handlers
   - Add functional tests (create scenarios with MenuTestDataFactory).

7) Implement AssignedToCategory handler
   - Add functional tests (assert alphabetical item order within category).

8) Implement Deleted handler
   - Ensure it uses IncludeSoftDeleted lookup to resolve RestaurantId.
   - Add functional tests.

9) Confirm DI registration (MediatR scanning) picks up handlers automatically (no extra wiring typically needed).

10) Optional enhancements (post-Phase C)
   - Add separate Search Index handlers for Created/Deleted/DetailsUpdated.
   - Consider small cache invalidation hook if a cache layer exists for FullMenuView.

## Risks and Considerations

- Resolving RestaurantId for soft-deleted items:
  - Must include soft-deleted in repository method; otherwise Deleted handler cannot rebuild proper restaurant view.
- Rebuild performance under frequent availability toggles:
  - Naive approach is acceptable now; monitor and consider coalescing/batching in outbox processing later.
- Concurrency:
  - Multiple events rebuilding the same restaurant: last write wins via lastRebuiltAt; Upsert is idempotent.
- Partial reads/caching:
  - If public endpoints cache GetFullMenuQuery responses, add a cache invalidation strategy post-Upsert in future work.

## Deliverables Overview

- Application files
  - src/Application/MenuItems/EventHandlers/Shared/MenuItemProjectorBase.cs
  - src/Application/MenuItems/EventHandlers/MenuItemCreatedEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemAvailabilityChangedEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemDetailsUpdatedEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemPriceChangedEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemDietaryTagsUpdatedEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemCustomizationAssignedEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemCustomizationRemovedEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemAssignedToCategoryEventHandler.cs
  - src/Application/MenuItems/EventHandlers/MenuItemDeletedEventHandler.cs

- Repository addition
  - src/Application/Common/Interfaces/IRepositories/IMenuItemRepository.cs: GetRestaurantIdByIdIncludingDeletedAsync
  - src/Infrastructure/Data/Repositories/MenuItemRepository.cs: implementation using IncludeSoftDeleted

- Functional tests
  - tests/Application.FunctionalTests/Features/MenuItems/Events/*EventHandlerTests.cs (one per event as above)

## Next Steps (Incremental)

- Implement repository helper and base projector.
- Build and test MenuItemCreated handler first to validate the end-to-end pattern with the existing FullMenuViewRebuilder.
- Proceed iteratively through the remaining handlers, adding tests per handler as described.

If you want, I can turn this into a short, prioritized Jira task breakdown with estimates and acceptance criteria per handler and test.

## Tasklist Status

- Investigation task completed. Immediate next actionable steps recommended:
  - Add the repository method for resolving RestaurantId including soft-deleted rows.
  - Implement MenuItemProjectorBase.
  - Implement and test MenuItemCreatedEventHandler as the initial vertical slice.
