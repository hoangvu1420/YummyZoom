# CustomizationGroup Aggregate

## Aggregate Documentation: `CustomizationGroup`

* **Version:** 1.1
* **Last Updated:** 2025-07-08
* **Source File:** `src/Domain/CustomizationGroupAggregate/CustomizationGroup.cs`

### 1. Overview

**Description:**
Manages a self-contained, reusable set of choices (e.g., sizes, toppings) that can be applied to menu items. This allows restaurant owners to define an option once and apply it to many menu items, with specific selection rules and pricing adjustments.

**Core Responsibilities:**

* Manages the lifecycle of customization groups and their choices
* Acts as the transactional boundary for all customization group operations
* Controls creation of CustomizationChoice entities to ensure proper encapsulation
* Enforces business rules for choice name uniqueness within the group
* Enforces business rules for valid selection range constraints (MaxSelections >= MinSelections)
* Manages display ordering of choices within the group

### 2. Structure

* **Aggregate Root:** `CustomizationGroup`
* **Key Child Entities:**
  * `CustomizationChoice`: Represents an individual choice within the group with name, price adjustment, default flag, and display order
* **Key Value Objects:**
  * `CustomizationGroupId`: Strongly-typed identifier for the aggregate
  * `ChoiceId`: Strongly-typed identifier for choices
  * `Money`: Represents price adjustments for choices

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Method)

The only valid way to create a `CustomizationGroup` is through its static factory method.

```csharp
public static Result<CustomizationGroup> Create(
    RestaurantId restaurantId,
    string groupName,
    int minSelections,
    int maxSelections,
    List<CustomizationChoice>? choices = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `restaurantId` | `RestaurantId` | The restaurant that owns this customization group |
| `groupName` | `string` | The name of the group (e.g., "Size", "Toppings") |
| `minSelections` | `int` | Minimum number of choices that must be selected |
| `maxSelections` | `int` | Maximum number of choices that can be selected |
| `choices` | `List<CustomizationChoice>?` | Optional initial choices for the group |

**Validation Rules & Potential Errors:**

* `groupName` cannot be null or empty. (Returns `CustomizationGroupErrors.GroupNameRequired`)
* `maxSelections` must be >= `minSelections`. (Returns `CustomizationGroupErrors.InvalidSelectionRange`)
* Group name uniqueness is enforced at the application/repository level

#### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result AddChoice(string name, Money priceAdjustment, bool isDefault, int displayOrder)` | Adds a new choice to the group with primitive parameters. The aggregate creates the CustomizationChoice entity internally. | Ensures choice names are unique within the group and validates all primitive parameters | `CustomizationGroupErrors.ChoiceNameNotUnique`, `CustomizationGroupErrors.ChoiceNameRequired`, `CustomizationGroupErrors.InvalidDisplayOrder` |
| `Result AddChoiceWithAutoOrder(string name, Money priceAdjustment, bool isDefault = false)` | Adds a new choice with auto-assigned display order | Ensures choice names are unique and assigns next available order | `CustomizationGroupErrors.ChoiceNameNotUnique`, `CustomizationGroupErrors.ChoiceNameRequired` |
| `Result RemoveChoice(ChoiceId choiceId)` | Removes a choice from the group by ID | Validates that the choice exists | `CustomizationGroupErrors.InvalidChoiceId` |
| `Result UpdateChoice(ChoiceId choiceId, string newName, Money newPriceAdjustment, bool isDefault, int? displayOrder = null)` | Updates an existing choice's properties | Ensures new name is unique and choice exists | `CustomizationGroupErrors.InvalidChoiceId`, `CustomizationGroupErrors.ChoiceNameNotUnique` |
| `Result ReorderChoices(List<(ChoiceId choiceId, int newDisplayOrder)> orderChanges)` | Bulk reorders multiple choices with new display orders | Validates choice IDs exist and display orders are non-negative | `CustomizationGroupErrors.ChoiceNotFoundForReordering`, `CustomizationGroupErrors.InvalidDisplayOrder`, `CustomizationGroupErrors.DuplicateDisplayOrder` |
| `Result UpdateGroupDetails(string groupName, int minSelections, int maxSelections)` | Updates group name and selection constraints | Validates name and selection range | `CustomizationGroupErrors.GroupNameRequired`, `CustomizationGroupErrors.InvalidSelectionRange` |
| `Result MarkAsDeleted()` | Marks the group as deleted | None - always succeeds | None |

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `CustomizationGroupId` | The unique identifier of the aggregate |
| `RestaurantId` | `RestaurantId` | The restaurant that owns this group |
| `GroupName` | `string` | The name of the customization group |
| `MinSelections` | `int` | Minimum number of choices that must be selected |
| `MaxSelections` | `int` | Maximum number of choices that can be selected |
| `Choices` | `IReadOnlyList<CustomizationChoice>` | Read-only collection of available choices, ordered by DisplayOrder ascending, then Name ascending |

#### 4.2. Public Query Methods

This aggregate does not expose any additional query methods beyond property access.

### 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `CustomizationGroupCreated` | During the `Create` factory method | Signals that a new customization group has been successfully created |
| `CustomizationChoiceAdded` | After a successful call to `AddChoice` or `AddChoiceWithAutoOrder` | Signals that a new choice was added to the group |
| `CustomizationChoiceRemoved` | After a successful call to `RemoveChoice` | Signals that a choice was removed from the group |
| `CustomizationChoiceUpdated` | After a successful call to `UpdateChoice` | Signals that an existing choice was modified |
| `CustomizationChoicesReordered` | After a successful call to `ReorderChoices` | Signals that multiple choices had their display order changed |
| `CustomizationGroupDeleted` | After a successful call to `MarkAsDeleted` | Signals that the group has been marked for deletion |
