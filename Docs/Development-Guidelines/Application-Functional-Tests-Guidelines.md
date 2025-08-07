## Guide to Writing Functional Tests in the YummyZoom Project

This document outlines how to write new functional tests for the YummyZoom application, leveraging the existing test infrastructure. Functional tests are crucial for verifying the end-to-end behavior of your application's features, including command/query handling, business logic, database interactions, and authorization.

**1. Test Project Structure**

The functional test project is organized into specialized layers for better maintainability:

```
tests/Application.FunctionalTests/
├── Infrastructure/           # Core test infrastructure
│   ├── TestInfrastructure.cs      # Setup, teardown, service management
│   ├── TestDatabaseManager.cs     # Database operations and entity management
│   └── ...
├── TestData/                # Centralized test data setup
│   ├── TestDataFactory.cs         # Core test data factory for default entities
│   ├── CouponTestDataFactory.cs   # Specialized factory for coupon scenarios
│   └── DefaultTestData.cs          # Default test data configuration
├── UserManagement/          # User creation and authentication
│   ├── TestUserManager.cs         # User operations and authentication
│   └── ...
├── Authorization/           # Authorization helpers and tests
│   ├── RestaurantRoleTestHelper.cs # Restaurant-specific role scenarios
│   └── ...
├── Features/                # Feature-specific tests organized by domain
│   └── ...
├── Common/                  # Shared utilities and base classes
│   ├── BaseTestFixture.cs         # Base test fixture
│   └── ...
└── Testing.cs               # Clean facade API for all test operations
```

**2. Understanding the Test Environment**

Our functional test environment is designed for reliability and isolation:

*   **Real Database (Testcontainers):** We use Testcontainers to spin up a dedicated PostgreSQL database instance in a Docker container for each full test suite run.
*   **In-Memory Application Host (`WebApplicationFactory`):** Tests interact with the application hosted in-memory, configured to connect to the Testcontainer database.
*   **Data Isolation (Respawner):** Between each test, `Respawner` wipes data from all tables, ensuring each test starts with a clean slate.
*   **Centralized Test Data:** A default set of test entities is created once per test suite run and automatically restored after each test, providing consistent baseline data.
*   **Unified Test API (`Testing.cs`):** A static `Testing` class provides a single, clean entry point for all common test operations.

**3. Key Infrastructure Components**

*   **`Testing.cs` (Unified Facade):** The primary entry point for all test operations.
    *   **Command/Query Execution:** `SendAsync()`, `SendAndUnwrapAsync()`
    *   **User Management:** `RunAsUserAsync()`, `RunAsDefaultUserAsync()`, `RunAsAdministratorAsync()`
    *   **Database Operations:** `FindAsync<TEntity>()`, `AddAsync<TEntity>()`, `CountAsync<TEntity>()`
    *   **Service Replacement:** `ReplaceService<TInterface>()`
    *   **Test Data Access:** `TestData.DefaultCustomerId`, `TestData.DefaultRestaurantId`, etc.
    *   **Authorization Helpers:** `CreateRoleAssignmentAsync()`, `RunAsRestaurantOwnerAsync()`

*   **Test Data Factories:**
    *   **`TestDataFactory.cs`:** Creates the default set of test data (user, restaurant, menu, items, coupon) once per suite. Also provides specialized methods for creating specific scenarios (e.g., `CreateInactiveRestaurantAsync()`).
    *   **`CouponTestDataFactory.cs`:** A specialized factory for creating various coupon test scenarios using a fluent `CouponTestOptions` builder.

**4. Writing a New Functional Test**

1.  **Create a Test File:** In the `Features/` directory, create a new class file in the appropriate domain folder (e.g., `Features/Orders/CreateOrderTests.cs`).
2.  **Inherit from `BaseTestFixture`:** This ensures per-test setup and teardown, including data reset.

    ```csharp
    using static YummyZoom.Application.FunctionalTests.Testing;

    public class CreateOrderTests : BaseTestFixture
    {
        // Your tests go here
    }
    ```

**5. Leveraging Test Data Factories**

The test data factories are the cornerstone of efficient and readable tests.

*   **Default Test Data:** For most tests, use the pre-created default entities.

    ```csharp
    [Test]
    public async Task CreateOrder_WithDefaultData_ShouldSucceed()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = TestData.DefaultCustomerId,
            RestaurantId = TestData.DefaultRestaurantId,
            MenuItemIds = TestData.GetMenuItemIds(TestData.MenuItems.ClassicBurger)
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }
    ```

*   **Specialized Test Scenarios:** When a test requires a specific state that deviates from the default, use the specialized factory methods.

    ```csharp
    [Test]
    public async Task InitiateOrder_ShouldFail_WhenRestaurantIsInactive()
    {
        // Arrange
        var inactiveRestaurantId = await TestDataFactory.CreateInactiveRestaurantAsync();
        var command = new InitiateOrderCommand
        {
            RestaurantId = inactiveRestaurantId,
            // ... other properties
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure(OrderErrors.RestaurantIsInactive);
    }
    ```

*   **Custom Coupon Scenarios:** Use `CouponTestDataFactory` to create specific coupon validation tests.

    ```csharp
    [Test]
    public async Task ApplyCoupon_ShouldFail_WhenCouponIsExpired()
    {
        // Arrange
        var expiredCouponCode = await CouponTestDataFactory.CreateExpiredCouponAsync();
        var command = new ApplyCouponCommand
        {
            CouponCode = expiredCouponCode,
            // ... other properties
        };

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure(CouponErrors.CouponIsInvalid);
    }
    ```

**6. Writing Test Methods**

Follow the Arrange-Act-Assert pattern, keeping tests focused on a single behavior.

```csharp
[Test]
public async Task CreateRestaurant_AsOwner_ShouldSucceed()
{
    // Arrange
    var userId = await RunAsUserAsync("owner@example.com", "password", new[] { Roles.RestaurantOwner });
    var command = new CreateRestaurantCommand { Name = "New Restaurant", ... };

    // Act
    var result = await SendAsync(command);

    // Assert
    result.ShouldBeSuccessful();
    var restaurant = await FindAsync<Restaurant>(result.Value);
    restaurant.Should().NotBeNull();
    restaurant.Name.Should().Be("New Restaurant");
}
```

**7. Key Considerations**

*   **Authentication:** For protected endpoints, always call `RunAs...Async()` before `SendAsync()`.
*   **Validation:** Test validation failures by sending invalid commands and asserting that a `ValidationException` is thrown.
*   **Readability:** Use the static `Testing` class and the test data factories to keep your tests clean, concise, and easy to understand.
*   **Focus:** Each test should verify a single, specific behavior. Avoid complex tests that try to do too much at once.

By following these guidelines, you can write effective and maintainable functional tests that ensure the quality and stability of the YummyZoom application.