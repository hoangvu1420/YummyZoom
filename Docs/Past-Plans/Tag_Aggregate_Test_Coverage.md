# Tag Aggregate - Unit Test Coverage Plan

## Test Structure Analysis

Based on existing test patterns in `tests/Domain.UnitTests/`, the tests will follow these conventions:
- **Framework:** NUnit with FluentAssertions
- **Organization:** `tests/Domain.UnitTests/TagAggregate/` folder
- **Naming:** `{Method}_{Condition}_{ExpectedBehavior}` pattern
- **Structure:** Arrange/Act/Assert with helper methods for test data

---

## 1. Tag Aggregate Root Tests (`TagTests.cs`)

### 1.1 Create() Method Tests

#### Happy Path:
- **`Create_WithValidInputs_ShouldSucceedAndInitializeTagCorrectly`**
  - ✅ Verify Result.IsSuccess = true
  - ✅ Verify all properties set correctly (TagName, TagCategory, TagDescription)
  - ✅ Verify TagId is generated (not empty Guid)
  - ✅ Verify TagCreated domain event is raised
  - ✅ Test with description = null (optional parameter)

#### Validation Failure Cases:
- **`Create_WithNullOrEmptyTagName_ShouldFailWithNameIsRequiredError`**
  - ✅ Test with null, empty string, and whitespace-only string
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.NameIsRequired
  - ✅ Verify no domain event raised

- **`Create_WithTagNameTooLong_ShouldFailWithNameTooLongError`**
  - ✅ Test with string > 100 characters
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.NameTooLong
  - ✅ Verify no domain event raised

- **`Create_WithNullOrEmptyCategory_ShouldFailWithCategoryIsRequiredError`**
  - ✅ Test with null, empty string, and whitespace-only string
  - ✅ Verify Result.IsFailure = true  
  - ✅ Verify Error = TagErrors.CategoryIsRequired
  - ✅ Verify no domain event raised

- **`Create_WithInvalidCategory_ShouldFailWithInvalidCategoryError`**
  - ✅ Test with category not in predefined list ("Invalid", "Random", etc.)
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.InvalidCategory
  - ✅ Verify no domain event raised

#### Valid Categories Test:
- **`Create_WithAllValidCategories_ShouldSucceed`**
  - ✅ Test each valid category: "Dietary", "Cuisine", "SpiceLevel", "Allergen", "Preparation", "Temperature"
  - ✅ Test case-insensitive matching ("dietary", "CUISINE", etc.)
  - ✅ Verify all succeed and create tag correctly

### 1.2 UpdateDetails() Method Tests

#### Happy Path:
- **`UpdateDetails_WithValidInputs_ShouldSucceedAndUpdateProperties`**
  - ✅ Create tag, then update with new name and description
  - ✅ Verify Result.IsSuccess = true
  - ✅ Verify TagName and TagDescription updated correctly
  - ✅ Verify TagUpdated domain event raised
  - ✅ Test with description = null

- **`UpdateDetails_WithSameName_ShouldSucceedButNotRaiseEvent`**
  - ✅ Create tag, then update with same name
  - ✅ Verify Result.IsSuccess = true
  - ✅ Verify properties updated
  - ✅ Verify NO new TagUpdated event raised (idempotent)

#### Validation Failure Cases:
- **`UpdateDetails_WithNullOrEmptyTagName_ShouldFailWithNameIsRequiredError`**
  - ✅ Test with null, empty string, whitespace-only
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.NameIsRequired
  - ✅ Verify original state unchanged
  - ✅ Verify no domain event raised

- **`UpdateDetails_WithTagNameTooLong_ShouldFailWithNameTooLongError`**
  - ✅ Test with string > 100 characters
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.NameTooLong
  - ✅ Verify original state unchanged
  - ✅ Verify no domain event raised

### 1.3 ChangeCategory() Method Tests

#### Happy Path:
- **`ChangeCategory_WithValidCategory_ShouldSucceedAndUpdateCategory`**
  - ✅ Create tag with one category, change to another valid category
  - ✅ Verify Result.IsSuccess = true
  - ✅ Verify TagCategory updated correctly
  - ✅ Verify TagUpdated domain event raised

- **`ChangeCategory_WithSameCategory_ShouldSucceedButNotRaiseEvent`**
  - ✅ Create tag, then change to same category
  - ✅ Verify Result.IsSuccess = true
  - ✅ Verify category unchanged
  - ✅ Verify NO new TagUpdated event raised (idempotent)

#### Validation Failure Cases:
- **`ChangeCategory_WithNullOrEmptyCategory_ShouldFailWithCategoryIsRequiredError`**
  - ✅ Test with null, empty string, whitespace-only
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.CategoryIsRequired
  - ✅ Verify original state unchanged
  - ✅ Verify no domain event raised

- **`ChangeCategory_WithInvalidCategory_ShouldFailWithInvalidCategoryError`**
  - ✅ Test with invalid category
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.InvalidCategory
  - ✅ Verify original state unchanged
  - ✅ Verify no domain event raised

---

## 2. TagId Value Object Tests (`TagIdTests.cs`)

### 2.1 CreateUnique() Method Tests
- **`CreateUnique_ShouldGenerateUniqueGuids`**
  - ✅ Call CreateUnique() multiple times
  - ✅ Verify each generates different Guid.Value
  - ✅ Verify no Guid is empty

### 2.2 Create(Guid) Method Tests
- **`Create_WithValidGuid_ShouldReturnTagIdWithCorrectValue`**
  - ✅ Create with specific Guid
  - ✅ Verify TagId.Value equals the input Guid

- **`Create_WithEmptyGuid_ShouldReturnTagIdWithEmptyGuid`**
  - ✅ Create with Guid.Empty
  - ✅ Verify TagId.Value is Guid.Empty (no validation in this method)

### 2.3 Create(string) Method Tests

#### Happy Path:
- **`Create_WithValidGuidString_ShouldSucceedAndReturnCorrectTagId`**
  - ✅ Test with valid GUID string
  - ✅ Verify Result.IsSuccess = true
  - ✅ Verify TagId.Value equals parsed Guid

#### Validation Failure Cases:
- **`Create_WithInvalidGuidString_ShouldFailWithInvalidTagIdError`**
  - ✅ Test with "not-a-guid", "12345", empty string, null
  - ✅ Verify Result.IsFailure = true
  - ✅ Verify Error = TagErrors.InvalidTagId

### 2.4 Equality Tests
- **`Equality_WithSameGuidValues_ShouldBeEqual`**
  - ✅ Create two TagIds with same Guid value
  - ✅ Verify tag1.Equals(tag2) = true
  - ✅ Verify tag1 == tag2 = true
  - ✅ Verify tag1.GetHashCode() == tag2.GetHashCode()

- **`Equality_WithDifferentGuidValues_ShouldNotBeEqual`**
  - ✅ Create two TagIds with different Guid values
  - ✅ Verify tag1.Equals(tag2) = false
  - ✅ Verify tag1 != tag2 = true
  - ✅ Verify different hash codes

- **`Equality_WithNull_ShouldNotBeEqual`**
  - ✅ Verify tagId.Equals(null) = false
  - ✅ Verify tagId != null = true

---

## 3. Domain Events Tests (`TagEventsTests.cs`)

### 3.1 TagCreated Event Tests
- **`TagCreated_ShouldInitializePropertiesCorrectly`**
  - ✅ Create TagCreated event with specific values
  - ✅ Verify TagId, TagName, TagCategory properties set correctly

### 3.2 TagUpdated Event Tests
- **`TagUpdated_ShouldInitializePropertiesCorrectly`**
  - ✅ Create TagUpdated event with specific values
  - ✅ Verify TagId, TagName, TagCategory properties set correctly

---

## 4. Test Data Helpers

### Constants:
```csharp
private const string DefaultTagName = "Vegetarian";
private const string DefaultTagDescription = "Contains no meat or animal products";
private const string DefaultTagCategory = "Dietary";
private const string LongTagName = new string('a', 101); // 101 characters
```

### Helper Methods:
```csharp
private static Tag CreateValidTag(string? name = null, string? category = null, string? description = null)
private static string[] GetValidCategories()
private static string[] GetInvalidCategories()
```

---

## 5. Test Organization Summary

```
tests/Domain.UnitTests/TagAggregate/
├── TagTests.cs (Aggregate Root - ~20 test methods)
├── TagIdTests.cs (Value Object - ~8 test methods)  
└── TagEventsTests.cs (Domain Events - ~2 test methods)
```

**Total Estimated Tests: ~30 test methods**

---

## 6. Coverage Validation

This test plan ensures:
- ✅ **100% method coverage** of public Tag aggregate methods
- ✅ **All business rules tested** with both success and failure cases
- ✅ **All validation scenarios covered** for input parameters
- ✅ **Domain events properly tested** for correct raising and content
- ✅ **State change verification** for all mutations
- ✅ **Idempotent behavior validation** for repeated operations
- ✅ **Value object equality and creation** thoroughly tested
- ✅ **Error propagation** correctly validated with specific error types

This coverage aligns with the Domain Layer Test Guidelines and follows established patterns from existing aggregate tests in the codebase. 