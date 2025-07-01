# Tag Aggregate Implementation Plan

## Analysis Summary

After examining the current domain structure, I've identified that:

1. **User**, **RoleAssignment**, and **Restaurant** aggregates are ✅ **fully implemented**
2. **Menu** aggregate is ✅ **already implemented** and matches the design blueprint well
3. **Tag** aggregate is ❌ **partially implemented** (only `TagId` value object exists)
4. **CustomizationGroup** aggregate is ❌ **partially implemented** (only `CustomizationGroupId` value object exists)

Since the Menu aggregate references both Tag and CustomizationGroup aggregates through their IDs, the next logical step is to implement the missing aggregate roots. The **Tag Aggregate** is the ideal next candidate because:

- It has no dependencies on other aggregates (self-contained)
- It's simpler than CustomizationGroup aggregate
- It's already referenced by the existing Menu aggregate

## Tag Aggregate Design Blueprint

Based on the Domain_Design.md specification:

### Aggregate Structure
- **Aggregate Root:** `Tag`
- **Description:** Manages centrally defined tags (e.g., for dietary preferences, cuisine styles) that can be applied across the system for classification, discovery, and filtering.

### Entities/Value Objects
- `Tag` (Entity - Root):
  - `TagID` (Identifier)
  - `TagName` (String, e.g., "Vegetarian", "Gluten-Free", "Spicy")
  - `TagDescription` (Optional)
  - `TagCategory` (String, e.g., "Dietary", "Cuisine", "SpiceLevel")

### Invariants
- `TagName` must be unique across the entire system to ensure consistency
- `TagName` cannot be null or empty
- `TagCategory` should be from a predefined set of valid categories

### References to Other Aggregates
- None. It is a self-contained, lookup-style aggregate.

## Implementation Plan

### 1. File Structure
The following files need to be created in `src/Domain/TagAggregate/`:

```
TagAggregate/
├── Tag.cs (Aggregate Root) - NEW
├── Events/
│   ├── TagCreated.cs - NEW
│   └── TagUpdated.cs - NEW (if needed)
├── Errors/
│   └── TagErrors.cs - NEW
└── ValueObjects/
    └── TagId.cs (already exists) ✅
```

### 2. Tag Aggregate Root Implementation

**File:** `src/Domain/TagAggregate/Tag.cs`

**Key Features:**
- Inherit from `AggregateRoot<TagId, Guid>`
- Private constructor + static factory method pattern
- Proper encapsulation with private setters
- Domain event raising for significant state changes
- Result-based error handling for operations that can fail

**Properties:**
- `string TagName` (required, unique across system)
- `string? TagDescription` (optional)
- `string TagCategory` (required, from predefined categories)

**Methods:**
- `static Result<Tag> Create(string tagName, string tagCategory, string? tagDescription = null)`
- `Result UpdateDetails(string tagName, string? tagDescription)`
- `Result ChangeCategory(string newTagCategory)`

### 3. Domain Events

**File:** `src/Domain/TagAggregate/Events/TagCreated.cs`
```csharp
public record TagCreated(TagId TagId, string TagName, string TagCategory) : IDomainEvent;
```

**File:** `src/Domain/TagAggregate/Events/TagUpdated.cs`
```csharp
public record TagUpdated(TagId TagId, string TagName, string TagCategory) : IDomainEvent;
```

### 4. Domain Errors

**File:** `src/Domain/TagAggregate/Errors/TagErrors.cs`

**Error Types:**
- `InvalidTagName` - for null/empty tag names
- `DuplicateTagName` - for uniqueness violations (may be handled at application layer)
- `InvalidTagCategory` - for invalid category values

### 5. Business Rules Implementation

#### Tag Categories Enum/Constants
Define predefined tag categories as constants or enum:
- "Dietary" (Vegetarian, Vegan, Gluten-Free, etc.)
- "Cuisine" (Italian, Chinese, Mexican, etc.)
- "SpiceLevel" (Mild, Medium, Hot, Extra Hot)
- "Allergen" (Contains Nuts, Contains Dairy, etc.)

#### Validation Logic
- Tag name validation (not null/empty, reasonable length)
- Category validation (must be from predefined list)
- Uniqueness validation (handled at application/repository level)

### 6. Integration Points

#### Current Integration
- Already referenced by `MenuItem` entities in Menu aggregate via `TagId` list
- `src/Domain/MenuAggregate/Entities/MenuItem.cs` uses `List<TagId> _dietaryTagIds`

#### Future Integration
- Can be extended for restaurant classification
- Potential use in search and filtering functionality
- May be referenced by future aggregates (e.g., for coupon targeting)

## Implementation Steps

1. **Create Tag Aggregate Root** (`Tag.cs`)
   - Implement following existing patterns in User/Restaurant aggregates
   - Include factory method, domain operations, and proper encapsulation

2. **Create Domain Events** (`Events/` folder)
   - Follow existing event patterns in Menu aggregate
   - Ensure events contain necessary data for event handlers

3. **Create Domain Errors** (`Errors/` folder)
   - Follow existing error patterns in other aggregates
   - Include all validation and business rule errors

4. **Verify TagId Value Object** (already exists)
   - Ensure it follows the same pattern as other ID value objects
   - Verify it has proper factory methods and validation

5. **Test Integration**
   - Verify Menu aggregate can properly reference Tag entities
   - Ensure domain events are properly raised and can be handled

## Benefits of This Implementation

1. **Completes Menu Aggregate Dependencies** - Menu aggregate can now properly reference Tag entities
2. **Establishes Foundation** - Creates reusable tagging system for the entire application
3. **Follows DDD Principles** - Self-contained aggregate with clear boundaries
4. **Enables Future Features** - Search, filtering, and categorization capabilities
5. **Maintains Consistency** - Centralized tag management ensures system-wide consistency

## Next Steps After Tag Aggregate

After implementing the Tag aggregate, the next logical aggregates to implement would be:

1. **CustomizationGroup Aggregate** - Completes Menu aggregate dependencies
2. **RestaurantAccount Aggregate** - For payouts and monetization
3. **Order Aggregate** - Core business functionality
4. **Coupon Aggregate** - Promotional features
5. **Review Aggregate** - Customer feedback
6. **SupportTicket Aggregate** - Customer support

This sequence ensures that dependencies are resolved before dependent aggregates are implemented. 