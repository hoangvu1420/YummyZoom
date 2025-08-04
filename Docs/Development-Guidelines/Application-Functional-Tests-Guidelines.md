## Guide to Writing Functional Tests in the YummyZoom Project

This document outlines how to write new functional tests for the YummyZoom application, leveraging the existing test infrastructure. Functional tests are crucial for verifying the end-to-end behavior of your application's features, including command/query handling, business logic, database interactions, and authorization.

**1. Test Project Structure**

The functional test project is organized into specialized layers for better maintainability:

```
tests/Application.FunctionalTests/
├── Infrastructure/           # Core test infrastructure
│   ├── TestInfrastructure.cs      # Setup, teardown, service management
│   ├── TestDatabaseManager.cs     # Database operations and entity management
│   ├── TestConfiguration.cs       # Centralized test constants and configuration
│   └── Database/                   # Database-specific implementations
├── UserManagement/          # User creation and authentication
│   ├── TestUserManager.cs         # User operations and basic authentication
│   └── TestAuthenticationService.cs # Authentication state and claims
├── Authorization/           # Authorization helpers and tests
│   ├── RestaurantRoleTestHelper.cs # Restaurant-specific role scenarios
│   ├── AuthorizationTestSetup.cs  # Common authorization setup
│   └── [authorization test files]  # Existing authorization tests
├── Features/                # Feature-specific tests organized by domain
│   ├── Users/
│   ├── Notifications/
│   ├── RoleAssignments/
│   └── [other feature folders]
├── Common/                  # Shared utilities and base classes
│   ├── BaseTestFixture.cs         # Base test fixture
│   ├── ResultAssertions.cs        # Result assertion helpers
│   └── CustomWebApplicationFactory.cs # Enhanced web application factory
└── Testing.cs               # Clean facade API for all test operations
```

**2. Understanding the Test Environment**

Our functional test environment is designed for reliability and isolation:

* **Real Database (Testcontainers):** We use Testcontainers to spin up a dedicated PostgreSQL database instance in a Docker container for each full test suite run. This ensures tests run against a real database matching our production environment.
* **In-Memory Application Host (`WebApplicationFactory`):** Tests interact with the application hosted in-memory using `CustomWebApplicationFactory`. This factory is configured to:
  * Connect to the Testcontainer database.
  * Allow mocking of certain services (like `IUser` to simulate authenticated users).
* **Data Isolation (Respawner):** Between each individual test method, the `Respawner` tool is used to quickly wipe data from all tables (except schema and migration history). This ensures each test starts with a clean data slate.
* **Centralized Test Helpers (`Testing.cs`):** Common operations like sending MediatR requests, simulating user logins, and direct database interaction are provided as static helper methods in `Testing.cs`.
* **Base Fixture (`Common/BaseTestFixture.cs`):** Test classes should inherit from `BaseTestFixture` to automatically get per-test setup (like data reset).

**3. Key Infrastructure Components**

* **`Testing.cs` (Unified Facade):**
  * **Test Infrastructure Setup:** `RunBeforeAnyTests()`, `RunAfterAnyTests()`, `ResetState()`
  * **Command/Query Execution:** `SendAsync<TResponse>()`, `SendAndUnwrapAsync<TResponse>()`
  * **User Management:** `RunAsUserAsync()`, `RunAsDefaultUserAsync()`, `RunAsAdministratorAsync()`, `CreateUserAsync()`
  * **Database Operations:** `FindAsync<TEntity>()`, `AddAsync<TEntity>()`, `CountAsync<TEntity>()`
  * **Authorization Helpers:** `CreateRoleAssignmentAsync()`, `RunAsRestaurantOwnerAsync()`, `SetupForAuthorizationTestsAsync()`

* **Infrastructure Layer:**
  * **`TestInfrastructure.cs`:** Core test setup, teardown, and service provider management
  * **`TestDatabaseManager.cs`:** Database operations and entity management with Respawner integration
  * **`TestConfiguration.cs`:** Centralized test constants (users, roles, database settings, test data)
  * **`Database/`:** Database implementations (`ITestDatabase`, `PostgreSQLTestcontainersTestDatabase`, etc.)

* **User Management Layer:**
  * **`TestUserManager.cs`:** User creation, authentication, and basic user operations
  * **`TestAuthenticationService.cs`:** Authentication state and claims management

* **Authorization Layer:**
  * **`RestaurantRoleTestHelper.cs`:** Restaurant-specific role assignments and test scenarios
  * **`AuthorizationTestSetup.cs`:** Common authorization test setup and configuration

* **Common Utilities:**
  * **`BaseTestFixture.cs`:** Base class for test fixtures with automatic state reset
  * **`ResultAssertions.cs`:** Custom assertions for `Result<T>` pattern
  * **`CustomWebApplicationFactory.cs`:** Enhanced web application factory with organized service registration

* **Constants:**
  * **`SharedKernel/Constants/Roles.cs` & `Policies.cs`:** Use these constants when specifying roles or policies for authorization
  * **`TestConfiguration`:** Centralized test-specific constants and configuration values

**4. Writing a New Functional Test Class**

1. **Create a New Test File:**
    * In the `Application.FunctionalTests/Features/` directory, create or navigate to the folder corresponding to your domain module (e.g., `Features/Restaurants/`, `Features/Orders/`).
    * Add a new C# class file (e.g., `CreateRestaurantTests.cs`).

2. **Inherit from `BaseTestFixture`:**

    ```csharp
    using YummyZoom.Application.Restaurants.Commands.CreateRestaurant; // Your command
    using YummyZoom.Domain.RestaurantAggregate; // Your domain entity
    using YummyZoom.SharedKernel.Constants;

    namespace YummyZoom.Application.FunctionalTests.Features.Restaurants;

    using static Testing; // Allows direct use of helper methods like SendAsync, RunAsUserAsync

    public class CreateRestaurantTests : BaseTestFixture
    {
        // Your tests will go here
    }
    ```

3. **Per-Test Setup (if needed):**
    * If your tests within this class require specific prerequisite data (like roles, or other seed data not handled by a generic user setup), add a `[SetUp]` method.

    ```csharp
    [SetUp]
    public async Task TestSpecificSetUp()
    {
        // Example: Ensure roles needed for restaurant operations exist
        await EnsureRolesExistAsync(Roles.RestaurantOwner, Roles.Administrator);
        // Example: If you have a method to seed a specific type of test data
        // await SeedCategoriesAsync();
    }
    ```

    * Remember, `ResetState()` from `BaseTestFixture` already runs before this `[SetUp]`.

**5. Writing Test Methods**

Follow the Arrange-Act-Assert pattern:

```csharp
[Test]
public async Task CreateRestaurant_AsRestaurantOwner_ShouldSucceedAndCreateRestaurant()
{
    // Arrange
    // 1. Simulate a user login (if the command requires authentication/authorization)
    var restaurantOwnerUserId = await RunAsUserAsync("owner@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.RestaurantOwner });

    // 2. Create the command with valid data
    var command = new CreateRestaurantCommand
    {
        Name = "The Testaurant",
        CuisineType = "Test Cuisine",
        // ... other properties
    };

    // Act
    // 3. Send the command
    var result = await SendAsync(command); // Assuming CreateRestaurantCommand returns Result<RestaurantId>

    // Assert
    // 4. Verify the result of the command execution
    result.ShouldBeSuccessful(); // Custom assertion from ResultAssertions.cs
    var createdRestaurantId = result.Value; // Or use result.ValueOrFail()
    createdRestaurantId.Should().NotBeNull(); // Or specific checks for the ID type

    // 5. Verify the state of the database (optional but recommended)
    var restaurantInDb = await FindAsync<Restaurant>(createdRestaurantId);
    restaurantInDb.Should().NotBeNull();
    restaurantInDb!.Name.Should().Be(command.Name);
    restaurantInDb.CuisineType.Should().Be(command.CuisineType);
    restaurantInDb.OwnerUserId.Should().Be(UserId.Create(restaurantOwnerUserId)); // If you store the owner ID

    // 6. Verify other side effects (e.g., domain events published - more advanced)
}
```

**6. Key Considerations When Writing Tests**

* **Authentication & Authorization:**
  * If your command/query is decorated with `[AuthorizeAttribute]`, you **must** use one of the `RunAs...Async()` methods before `SendAsync()` to simulate an authenticated user.
  * Ensure the roles/policies you use in `RunAs...Async()` match those required by the `[AuthorizeAttribute]`.
  * If no user is simulated (`_userId` is null), the mocked `IUser.Id` will be null, and the `AuthorizationBehaviour` will throw an `UnauthorizedAccessException` for protected endpoints.
  * If the user does not have the required role, a `ForbiddenAccessException` will be thrown.
* **Prerequisite Data (Roles, etc.):**
  * The `IdentityService.CreateUserAsync` (when called by your application's registration endpoint) assigns a default `Roles.User` to new users.
  * For other roles, ensure they exist using `EnsureRolesExistAsync()` in your test's `[SetUp]` method or in a specific setup helper like `SetupForUserRegistrationTestsAsync()`. This method is idempotent (won't try to create a role if it already exists).
* **Database Interactions:**
  * Use `FindAsync<TEntity>(id)` to retrieve entities for verification.
  * Use `AddAsync<TEntity>(entity)` if you need to seed specific prerequisite entities directly (though often it's better to do this via commands if possible to test more of the system).
* **Validations:**
  * Test validation failures by sending commands with invalid data. These should typically throw a `ValidationException` (from the `ValidationBehaviour` in MediatR pipeline).

    ```csharp
    [Test]
    public async Task CreateRestaurant_WithMissingName_ShouldFailValidation()
    {
        // Arrange
        await RunAsUserAsync("owner@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.RestaurantOwner });
        var command = new CreateRestaurantCommand { Name = null, /* ... */ };

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
    ```

* **`Result` Pattern:**
  * Many commands/queries will return `Result<T>` or `Result`. Use the custom assertions in `Common/ResultAssertions.cs` (e.g., `ShouldBeSuccessful()`, `ShouldBeFailure()`, `ValueOrFail()`).
* **Test Configuration:**
  * Use `TestConfiguration` constants instead of hardcoded values:
    * `TestConfiguration.DefaultUsers.Email` for default test user email
    * `TestConfiguration.DefaultUsers.CommonTestPassword` for test passwords
    * `TestConfiguration.Roles.*` for role names
    * `TestConfiguration.TestData.*` for common test data values
* **Idempotency of Setup:** Try to make your test setup steps (especially data seeding) idempotent if they might be called multiple times. `EnsureRolesExistAsync` is an example.

**7. Running Functional Tests**

* Ensure Docker is running if you are using `PostgreSQLTestcontainersTestDatabase`.
* Run tests via your IDE's test runner or the `dotnet test` command.

**8. Troubleshooting Common Issues**

* **`System.InvalidOperationException: Role X does not exist.`:**
  * Ensure `await EnsureRolesExistAsync("X")` or a broader setup method that includes role "X" is called in your test's `[SetUp]` or a relevant helper before the code that assigns/checks role "X" executes.
* **`UnauthorizedAccessException`:**
  * You likely forgot to call `RunAsUserAsync()` (or similar) before sending a command that requires authentication.
* **`ForbiddenAccessException`:**
  * The simulated user (from `RunAsUserAsync()`) does not have the required roles or does not satisfy the policy for the command being sent. Check the `[AuthorizeAttribute]` on your command and the roles/policies assigned to the user in your test.
* **Database Connection Issues:**
  * Verify Docker is running and the Testcontainer is starting correctly (check test output).
  * Ensure the connection string in `CustomWebApplicationFactory` is correctly pointing to the test database.
* **Slow Tests:**
  * If `ApplicationDbContextInitialiser` was being used extensively in test setup and made tests slow, moving to targeted seeding (like `EnsureRolesExistAsync`) is the right approach.
  * Avoid excessive database operations in tests if possible; focus on verifying the specific feature.

By following these guidelines, you can write effective functional tests that contribute to the stability and quality of the YummyZoom application. Remember to keep tests focused on a single behavior or feature where possible.
