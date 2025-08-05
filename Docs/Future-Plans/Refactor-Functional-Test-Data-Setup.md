
# Refactoring Functional Test Data Setup

**Date:** 2025-08-05

## 1. Introduction

This document outlines a plan to refactor the test data setup mechanism within the `tests/Application.FunctionalTests` project. The current approach, as seen in `OnlineOrderPaymentTests.cs`, involves setting up test data directly within the `[SetUp]` method of each test fixture. This leads to code duplication, reduced maintainability, and potentially slower test execution.

The goal is to create a centralized, reusable, and efficient test data seeding module that integrates seamlessly with our existing test infrastructure, following the principles laid out in `Docs/Development-Guidelines/Application-Functional-Tests-Guidelines.md`.

## 2. Analysis of the Current Approach

The `OnlineOrderPaymentTests.cs` file demonstrates the current pattern:

- **In-Fixture Setup:** The `SetUp` method is responsible for creating a user, restaurant, menu, menu items, and a coupon.
- **Direct Database Insertion:** Entities are created and then added directly to the database using `AddAsync()`.
- **State Management:** Test-specific state (like `_customerId`, `_restaurantId`) is stored in private fields within the test fixture.
- **Repetitive Code:** This setup logic would need to be duplicated or slightly modified for any other test requiring a similar set of entities.
- **Performance:** While `Respawner` cleans the database between tests, the setup logic runs for every test fixture, which can be inefficient if the same foundational data is required across multiple test classes.

The existing guidelines in `Application-Functional-Tests-Guidelines.md` advocate for centralized helpers (`Testing.cs`) and a structured test project, which the current data setup approach only partially follows.

## 3. Proposed Design: A Centralized Test Data Factory

We will introduce a new, centralized module for managing test data. This module will be responsible for seeding the database with a default set of entities that can be shared across multiple tests.

### 3.1. New Project Structure

A new folder will be created to house the data factory components:

```
tests/Application.FunctionalTests/
├── TestData/
│   ├── TestDataFactory.cs
│   └── DefaultTestData.cs
```

### 3.2. Component Breakdown

#### `DefaultTestData.cs`

This static class will act as a configuration store for the default entities. It will contain constants and static properties that define the data, but not the entities themselves. This keeps the "what" (the data) separate from the "how" (the creation logic).

```csharp
// Example Structure
public static class DefaultTestData
{
    public static class Restaurant
    {
        public const string Name = "YummyZoom Test Kitchen";
        public const string Cuisine = "Fusion";
        // ... other properties
    }

    public static class Customer
    {
        public const string Email = "default-customer@testing.com";
        // ... other properties
    }

    public static class MenuItems
    {
        public static readonly (string Name, decimal Price) Burger = ("Classic Burger", 12.99m);
        public static readonly (string Name, decimal Price) Pizza = ("Margherita Pizza", 15.50m);
    }
}
```

#### `TestDataFactory.cs`

This will be the core of the new module. It will be a static class responsible for creating and persisting the entities defined in `DefaultTestData`. It will manage the state of the seeded data to ensure it's only created once per test run.

```csharp
// Example Structure
public static class TestDataFactory
{
    private static bool _isInitialized;
    private static readonly AsyncLock _lock = new();

    // Properties to hold the created entity IDs and other relevant data
    public static Guid DefaultCustomerId { get; private set; }
    public static Guid DefaultRestaurantId { get; private set; }
    public static List<Guid> DefaultMenuItemIds { get; } = new();

    /// <summary>
    /// Initializes and seeds the database with default test data.
    /// This method is designed to be called once per test suite run.
    /// </summary>
    public static async Task InitializeAsync()
    {
        using (await _lock.LockAsync())
        {
            if (_isInitialized)
                return;

            // 1. Create and save the default user
            DefaultCustomerId = await Testing.RunAsUserAsync(
                DefaultTestData.Customer.Email, 
                TestConfiguration.DefaultUsers.CommonTestPassword, 
                new[] { Roles.User });

            // 2. Create and save the default restaurant
            var restaurant = Restaurant.Create(...); // Using data from DefaultTestData
            await Testing.AddAsync(restaurant);
            DefaultRestaurantId = restaurant.Id.Value;

            // 3. Create and save menu items
            // ... logic to create and add menu, category, and items ...
            
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Resets the factory's state. Should be called after all tests have run.
    /// </summary>
    public static void Reset()
    {
        _isInitialized = false;
        DefaultMenuItemIds.Clear();
    }
}
```

### 3.3. Integration with the Test Framework

1.  **Initialization:** The `TestInfrastructure.cs` class will be modified to call `TestDataFactory.InitializeAsync()` within its `RunBeforeAnyTests()` method. This ensures the data is seeded before any test fixtures are run. It will also call `TestDataFactory.Reset()` in `RunAfterAnyTests()`.

2.  **Test Fixture Refactoring:** The `OnlineOrderPaymentTests.cs` and other similar test fixtures will be refactored to remove the complex `SetUp` logic. Instead, they will directly access the seeded data from the `TestDataFactory`.

    **Before:**

    ```csharp
    public class OnlineOrderPaymentTests : BaseTestFixture
    {
        private Guid _customerId;
        private Guid _restaurantId;
        // ...

        [SetUp]
        public async Task SetUp()
        {
            // ... complex setup logic ...
        }

        [Test]
        public async Task MyTest()
        {
            var command = new MyCommand { CustomerId = _customerId, ... };
            // ...
        }
    }
    ```

    **After:**

    ```csharp
    public class OnlineOrderPaymentTests : BaseTestFixture
    {
        [Test]
        public async Task MyTest()
        {
            // Arrange
            var command = new MyCommand 
            { 
                CustomerId = TestDataFactory.DefaultCustomerId,
                RestaurantId = TestDataFactory.DefaultRestaurantId,
                MenuItemId = TestDataFactory.DefaultMenuItemIds.First(),
                // ...
            };

            // Act & Assert
            // ...
        }
    }
    ```

## 4. Benefits of the New Approach

-   **Modularity & Reusability:** Test data setup is encapsulated in a single, dedicated module that can be used by any test.
-   **Maintainability:** Changes to the default test data only need to be made in one place (`TestData` and `TestDataFactory`).
-   **Improved Performance:** The database is seeded only once per test suite run, significantly reducing the overhead for each test fixture.
-   **Readability:** Test methods become cleaner and more focused on the actual test logic, as the setup is abstracted away.
-   **Consistency:** Ensures all tests run against the same baseline data, leading to more predictable and reliable test outcomes.
-   **Alignment with Guidelines:** This design aligns perfectly with the established patterns of using centralized helpers and maintaining a clean, structured test project.

## 5. Implementation Steps

1.  Create the `tests/Application.FunctionalTests/TestData` directory.
2.  Implement the `DefaultTestData.cs` static class with the required constants.
3.  Implement the `TestDataFactory.cs` static class with the `InitializeAsync` and `Reset` methods.
4.  Update `TestInfrastructure.cs` to call the `TestDataFactory` methods.
5.  Refactor `OnlineOrderPaymentTests.cs` to use the new `TestDataFactory`.
6.  Identify and refactor other test fixtures that can benefit from the centralized data setup.
