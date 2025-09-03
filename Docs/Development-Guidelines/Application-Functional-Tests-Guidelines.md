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
│   ├── CouponTestDataFactory.cs   # Specialized factory for coupon scenarios (options-based)
│   ├── MenuTestDataFactory.cs     # Specialized factory for menu scenarios (options-based)
│   └── DefaultTestData.cs         # Default test data configuration
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

*   **`Testing.cs` (Unified Facade):** The single, minimal API for tests.
    *   **Commands/Queries:** `SendAsync(request)`, `SendAndUnwrapAsync(request)`
    *   **Outbox (deterministic side-effects):** `DrainOutboxAsync()`, `ProcessOutboxOnceAsync()`
    *   **Users/Auth:** `RunAsUserAsync(...)`, `RunAsDefaultUserAsync()`, `RunAsAdministratorAsync()`, `RunAsRestaurantOwnerAsync()`
    *   **DB Ops:** `AddAsync(entity)`, `UpdateAsync(entity)`, `FindAsync<TEntity>(id)`, `CountAsync<TEntity>()`
    *   **DI Overrides:** `ReplaceService<TInterface>(replacement)`
    *   **Test Data Snapshot:** `TestData.Default*` ids and helpers

*   **Test Data Factories:**
    *   **`TestDataFactory.cs`:** Creates the default set of test data (user, restaurant, menu, items, coupon) once per suite. Also provides specialized methods for creating specific scenarios (e.g., `CreateInactiveRestaurantAsync()`).
    *   **`CouponTestDataFactory.cs`:** Options-based factory for coupon scenarios.
    *   **`MenuTestDataFactory.cs`:** Options-based factory for menu scenarios (enabled/disabled menu, categories, items, tag links, customization links, soft-deletes) returning IDs for assertions.

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

*   **Custom Menu Scenarios (options-based):** Use `MenuTestDataFactory` for complex menu/category/item/tag/group arrangements.

    ```csharp
    [Test]
    public async Task RebuildFullMenu_ShouldComposeGroupsTagsAndOrdering()
    {
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            EnabledMenu = true,
            CategoryCount = 2,
            CategoryGenerator = i => ($"Cat-{i}", i + 1),
            ItemGenerator = (categoryId, index) => new []
            {
                new ItemOptions { Name = $"Item-{index}-A", PriceAmount = 9.99m },
                new ItemOptions { Name = $"Item-{index}-B", PriceAmount = 12.50m }
            }
        });

        var result = await SendAsync(new RebuildFullMenuCommand { RestaurantId = scenario.RestaurantId });
        result.ShouldBeSuccessful();
    }
    ```

**6. Writing Test Methods**

Follow the Arrange-Act-Assert pattern, keeping tests focused on a single behavior. Comments should be used to explain the purpose of each section in a clear and concise manner.

```csharp
[Test]
public async Task CreateRestaurant_AsOwner_ShouldSucceed()
{
    // Arrange
    var userId = await RunAsUserAsync("owner@example.com", "password", new[] { Roles.RestaurantOwner });
    var command = new CreateRestaurantCommand { Name = "New Restaurant", ... };

    // Act
    var result = await SendAsync(command);
    // If the command emits domain events that drive side-effects (logs, projections, notifications, etc.)
    // drain the outbox before asserting those side-effects.
    await DrainOutboxAsync(); // Act → Drain → Assert

    // Assert
    result.ShouldBeSuccessful();
    var restaurant = await FindAsync<Restaurant>(result.Value);
    restaurant.Should().NotBeNull();
    restaurant.Name.Should().Be("New Restaurant");
}
```

Test names should follow the pattern:

- `[Action]_[Condition]_Should[ExpectedResult]` (e.g., `CreateOrder_WithDefaultData_ShouldSucceed`)
- `[Action]_Should[ExpectedResult]_When[Condition]` (e.g., `InitiateOrder_ShouldFail_WhenRestaurantIsInactive`)

**7. Key Considerations**

*   **Authentication:** For protected endpoints, always call `RunAs...Async()` before `SendAsync()`.
*   **Validation:** Test validation failures by sending invalid commands and asserting that a `ValidationException` is thrown.
*   **Event-Driven Assertions (Outbox/Inbox):** When a command enqueues domain events whose handlers produce side-effects you assert, call `await DrainOutboxAsync()` after `SendAsync(...)` and before assertions. This makes tests deterministic without sleeps or timing flakiness. Leaving background hosted services enabled is fine; draining ensures completion.
*   **Readability:** Use the static `Testing` class and the test data factories to keep your tests clean, concise, and easy to understand.
*   **Focus:** Each test should verify a single, specific behavior. Avoid complex tests that try to do too much at once.

**8. Debugging Event-Driven Tests**

**Common Pitfall: JSON Deserialization Failures**

**Problem**: Events reach outbox but handlers never execute.

**Root Cause**: Value objects lack `[JsonConstructor]` attribute.

**Symptoms**: 
- `DrainOutboxAsync()` completes silently
- Event handlers not called
- Tests fail expecting handler side-effects

**Solution**: Add `[JsonConstructor]` to value object constructors:

```csharp
public sealed class Rating : ValueObject
{
    public int Value { get; private set; }
    
    [JsonConstructor]  // Required for JSON deserialization
    private Rating() { }
    
    private Rating(int value) => Value = value;
    public static Result<Rating> Create(int value) => ...;
}
```

**Debugging**: Add debug output to `OutboxProcessor.ProcessOnceAsync()` to catch deserialization exceptions.

By following these guidelines, you can write effective and maintainable functional tests that ensure the quality and stability of the YummyZoom application.