# CustomizationGroup Aggregate Implementation Plan

## Analysis Summary

After reviewing the current domain structure and the blueprint in `Docs/Design/Domain_Design.md`, the following is established:

1. **User**, **RoleAssignment**, **Restaurant**, **Menu**, and **Tag** aggregates are ✅ **fully implemented**
2. **CustomizationGroup** aggregate is ❌ **not yet implemented** (only `CustomizationGroupId` value object exists)

The **CustomizationGroup** aggregate is referenced by the Menu aggregate (via `AppliedCustomizations`) and is essential for reusable menu options (e.g., toppings, sizes). Implementing this aggregate will complete a key dependency for menu customization features.

## CustomizationGroup Aggregate Design Blueprint

### Aggregate Structure
- **Aggregate Root:** `CustomizationGroup`
- **Description:** Manages a self-contained, reusable set of choices (e.g., sizes, toppings, add-ons) for a restaurant.

### Entities/Value Objects
- `CustomizationGroup` (Aggregate Root):
  - `CustomizationGroupId` (Identifier, already exists)
  - `RestaurantId` (Reference to Restaurant)
  - `GroupName` (string, unique within restaurant)
  - `MinSelections` (int)
  - `MaxSelections` (int)
  - List of `CustomizationChoice` (child entity)
- `CustomizationChoice` (Entity):
  - `ChoiceId` (new Value Object, Guid)
  - `Name` (string, unique within group)
  - `PriceAdjustment` (Money VO, can be zero)
  - `IsDefault` (bool)

### Invariants
- `GroupName` must be unique within the restaurant.
- `MaxSelections` >= `MinSelections`.
- `CustomizationChoice.Name` must be unique within the group.

### References to Other Aggregates
- `RestaurantId` (Restaurant aggregate)

---

## Implementation Plan

### 1. File Structure
Create the following files in `src/Domain/CustomizationGroupAggregate/`:

```
CustomizationGroupAggregate/
├── CustomizationGroup.cs (Aggregate Root) - NEW
├── Entities/
│   └── CustomizationChoice.cs - NEW
├── Events/
│   ├── CustomizationGroupCreated.cs - NEW
│   └── CustomizationChoiceAdded.cs - NEW (optional)
├── Errors/
│   └── CustomizationGroupErrors.cs - NEW
└── ValueObjects/
    ├── CustomizationGroupId.cs (already exists)
    └── ChoiceId.cs - NEW
```

### 2. CustomizationGroup Aggregate Root

- Inherit from `AggregateRoot<CustomizationGroupId, Guid>`
- Private constructor + static factory method
- Encapsulate state with private setters and collections
- Expose choices as `IReadOnlyList<CustomizationChoice>`
- Domain event raising for significant changes (creation, adding/removing choices)
- Result-based error handling for operations that can fail

**Properties:**
- `CustomizationGroupId Id`
- `RestaurantId RestaurantId`
- `string GroupName`
- `int MinSelections`
- `int MaxSelections`
- `IReadOnlyList<CustomizationChoice> Choices`

**Methods:**
- `static Result<CustomizationGroup> Create(...)`
- `Result AddChoice(...)`
- `Result RemoveChoice(ChoiceId id)`
- `Result UpdateChoice(...)`
- `Result UpdateGroupDetails(...)`

### 3. CustomizationChoice Entity

- Inherit from `Entity<ChoiceId>`
- Properties: `ChoiceId`, `Name`, `Money PriceAdjustment`, `bool IsDefault`
- Private constructor + static factory method
- Validation for name uniqueness within group

### 4. Value Objects

- `ChoiceId` (similar to TagId/MenuCategoryId)
- Use existing `Money` VO for `PriceAdjustment`

### 5. Domain Events

- `CustomizationGroupCreated` (Id, RestaurantId, GroupName)
- `CustomizationChoiceAdded` (GroupId, ChoiceId, Name) (optional)

### 6. Domain Errors

- `CustomizationGroupErrors` static class
  - `GroupNameRequired`
  - `GroupNameNotUnique`
  - `InvalidSelectionRange`
  - `ChoiceNameNotUnique`
  - `InvalidChoiceId`
  - etc.

### 7. Business Rules Implementation

- Validate group name (not null/empty, unique within restaurant)
- Validate selection range (`MaxSelections` >= `MinSelections`)
- Validate choice name uniqueness within group
- Validate price adjustment (Money VO, non-negative)

### 8. Integration Points

- Referenced by Menu aggregate via `AppliedCustomizations`
- Used in menu item customization flows

### 9. Implementation Steps

1. Create `ChoiceId` value object
2. Implement `CustomizationChoice` entity
3. Implement `CustomizationGroup` aggregate root
4. Implement domain events
5. Implement domain errors
6. Add unit tests for invariants and behaviors

---

## Benefits

- Enables reusable, consistent customization options for menu items
- Follows DDD and codebase conventions
- Supports future extensibility (e.g., new choice types, validation rules)
- Decouples customization logic from menu items

---

## Next Steps After CustomizationGroup

- Implement RestaurantAccount aggregate (for payouts)
- Implement Order aggregate (core business flow)
- Implement Coupon, Review, and SupportTicket aggregates 