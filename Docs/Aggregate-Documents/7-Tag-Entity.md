# Tag Entity

## Entity Documentation: `Tag`

* **Version:** 1.0
* **Last Updated:** 2024-12-13
* **Source File:** `src/Domain/TagEntity/Tag.cs`

### 1. Overview

**Description:**
A simple, centrally defined entity for classification (e.g., "Vegetarian", "Spicy"). It does not require the overhead of an aggregate as its invariants are simple. Tags are used to classify menu items and provide searchable metadata for filtering and discovery.

**Core Responsibilities:**

* Manages the lifecycle of classification tags
* Acts as a simple entity for categorizing menu items
* Enforces business rules for tag name uniqueness and length constraints
* Enforces business rules for valid tag categories

### 2. Structure

* **Entity Root:** `Tag`
* **Key Value Objects:**
  * `TagId`: Strongly-typed identifier for the tag
  * `TagCategory`: Enumeration of valid tag categories (Dietary, Cuisine, SpiceLevel, etc.)

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Method)

The only valid way to create a `Tag` is through its static factory method.

```csharp
public static Result<Tag> Create(
    string tagName,
    TagCategory tagCategory,
    string? tagDescription = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `tagName` | `string` | The name of the tag (e.g., "Vegetarian", "Spicy") |
| `tagCategory` | `TagCategory` | The category this tag belongs to |
| `tagDescription` | `string?` | Optional description providing more details about the tag |

**Validation Rules & Potential Errors:**

* `tagName` cannot be null or empty. (Returns `TagErrors.NameIsRequired`)
* `tagName` cannot exceed 100 characters. (Returns `TagErrors.NameTooLong`)
* Tag name uniqueness is enforced at the application/repository level

#### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the entity. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result UpdateDetails(string tagName, string? tagDescription)` | Updates the tag's name and description | Validates name is required and within length limits | `TagErrors.NameIsRequired`, `TagErrors.NameTooLong` |
| `Result ChangeCategory(TagCategory newTagCategory)` | Changes the tag's category | Validates the category is a valid enum value | None (enum validation is compile-time) |
| `Result MarkAsDeleted()` | Marks the tag as deleted | None - always succeeds | None |

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `TagId` | The unique identifier of the tag |
| `TagName` | `string` | The name of the tag |
| `TagDescription` | `string?` | Optional description of the tag |
| `TagCategory` | `TagCategory` | The category this tag belongs to |

#### 4.2. Public Query Methods

This entity does not expose any additional query methods beyond property access.

### 5. Communication (Domain Events)

The entity raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `TagCreated` | During the `Create` factory method | Signals that a new tag has been successfully created |
| `TagUpdated` | After a successful call to `UpdateDetails` (only if name changed) | Signals that the tag's name was modified |
| `TagCategoryChanged` | After a successful call to `ChangeCategory` (only if category changed) | Signals that the tag's category was changed |
| `TagDeleted` | After a successful call to `MarkAsDeleted` | Signals that the tag has been marked for deletion |
