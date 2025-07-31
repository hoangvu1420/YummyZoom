# Application Functional Tests Refactoring Plan

## Current State Analysis

The current `Testing.cs` file in the Application.FunctionalTests project has grown to 356 lines and contains multiple responsibilities that violate the Single Responsibility Principle. While the functionality works correctly, the code suffers from several maintainability and readability issues.

### Current Issues Identified

1. **Monolithic Testing Class**: The `Testing.cs` file contains too many responsibilities:
   - Test infrastructure setup/teardown
   - User management and authentication
   - Role assignment creation
   - Database operations
   - Service provider access
   - Authorization test helpers

2. **Mixed Abstraction Levels**: The class mixes low-level operations (direct database access) with high-level test scenarios (user role setup)

3. **Poor Separation of Concerns**: Authentication, authorization, database operations, and test infrastructure are all mixed together

4. **Limited Reusability**: Many helper methods are tightly coupled, making them difficult to reuse in different test scenarios

5. **Unclear Dependencies**: The relationships between different helper methods are not immediately obvious

6. **Inconsistent Naming**: Some methods follow different naming conventions

## Proposed Refactoring Structure

### 1. Core Infrastructure Layer

#### `TestInfrastructure.cs`

- **Responsibility**: Core test setup, teardown, and service provider management
- **Methods**:
  - `RunBeforeAnyTests()` / `RunAfterAnyTests()`
  - `SendAsync<T>()` / `SendAndUnwrapAsync<T>()`
  - `GetService<T>()`
  - `CreateScope()`
  - `ResetState()`

#### `TestDatabaseManager.cs`

- **Responsibility**: Database-specific operations and entity management
- **Methods**:
  - `FindAsync<TEntity>()`
  - `AddAsync<TEntity>()`
  - `CountAsync<TEntity>()`
  - Database reset and cleanup operations

### 2. User Management Layer

#### `TestUserManager.cs`

- **Responsibility**: User creation, authentication, and basic user operations
- **Methods**:
  - `CreateUserAsync(email, password, roles[])`
  - `RunAsUserAsync(email, password, roles[])`
  - `RunAsDefaultUserAsync()`
  - `RunAsAdministratorAsync()`
  - `EnsureRolesExistAsync(roles[])`
  - `GetCurrentUserId()`
  - `SetCurrentUserId(userId)`

#### `TestAuthenticationService.cs`

- **Responsibility**: Authentication state management and claims handling
- **Methods**:
  - `RefreshUserClaimsAsync()`
  - `AddPermissionClaim(role, resourceId)`
  - `RemovePermissionClaim(role, resourceId)`
  - `ClearAuthenticationState()`

### 3. Authorization Test Helpers Layer

#### `RestaurantRoleTestHelper.cs`

- **Responsibility**: Restaurant-specific role assignments and authorization scenarios
- **Methods**:
  - `CreateRoleAssignmentAsync(userId, restaurantId, role)`
  - `RunAsRestaurantOwnerAsync(email, restaurantId)`
  - `RunAsRestaurantStaffAsync(email, restaurantId)`
  - `RunAsUserWithMultipleRestaurantRolesAsync(email, roleAssignments[])`
  - `SetupRestaurantAuthorizationTestsAsync()`

#### `AuthorizationTestSetup.cs`

- **Responsibility**: Common authorization test setup and configuration
- **Methods**:
  - `SetupForAuthorizationTestsAsync()`
  - `SetupForUserRegistrationTestsAsync()`
  - `CreateTestRestaurantsAsync(count)`
  - `CleanupAuthorizationTestDataAsync()`

### 4. Test Configuration and Factory Layer

#### `TestConfiguration.cs`

- **Responsibility**: Test-specific configuration and constants
- **Properties**:
  - Default test user credentials
  - Test role definitions
  - Common test data constants

#### Enhanced `CustomWebApplicationFactory.cs`

- **Responsibility**: Web application factory with better service registration organization
- **Improvements**:
  - Separate service registration methods by concern
  - Better mock service organization
  - Clearer test service configuration

### 5. Facade Layer

#### `Testing.cs` (Refactored)

- **Responsibility**: Provide a clean, unified API for test classes
- **Structure**: Static facade that delegates to appropriate specialized classes
- **Methods**: Simplified, well-organized public API that hides implementation complexity

## Refactoring Benefits

### 1. Improved Maintainability

- **Single Responsibility**: Each class has a clear, focused purpose
- **Easier Debugging**: Issues can be isolated to specific components
- **Simpler Testing**: Individual components can be unit tested

### 2. Enhanced Readability

- **Clear Naming**: Class and method names clearly indicate their purpose
- **Logical Grouping**: Related functionality is grouped together
- **Reduced Cognitive Load**: Developers can focus on specific concerns

### 3. Better Reusability

- **Modular Design**: Components can be reused across different test scenarios
- **Flexible Composition**: Test scenarios can combine different helpers as needed
- **Extensibility**: New functionality can be added without modifying existing code

### 4. Improved Separation of Concerns

- **Infrastructure vs. Business Logic**: Clear separation between test infrastructure and business test logic
- **Authentication vs. Authorization**: Distinct handling of authentication and authorization concerns
- **Database vs. Application**: Clear boundaries between data access and application logic

## Final Folder Structure After Refactoring

```
tests/Application.FunctionalTests/
├── Infrastructure/
│   ├── TestInfrastructure.cs           # Core test setup, teardown, and service provider management
│   ├── TestDatabaseManager.cs          # Database operations and entity management
│   ├── TestConfiguration.cs            # Test-specific configuration and constants
│   └── Database/
│       ├── ITestDatabase.cs             # (existing) Database interface
│       ├── TestDatabaseFactory.cs       # (existing) Database factory
│       ├── PostgreSQLTestDatabase.cs    # (existing) Local PostgreSQL implementation
│       └── PostgreSQLTestcontainersTestDatabase.cs # (existing) Testcontainers implementation
├── UserManagement/
│   ├── TestUserManager.cs               # User creation, authentication, and basic operations
│   └── TestAuthenticationService.cs     # Authentication state and claims management
├── Authorization/
│   ├── RestaurantRoleTestHelper.cs      # Restaurant-specific role assignments and scenarios
│   ├── AuthorizationTestSetup.cs        # Common authorization test setup and configuration
│   ├── TestRestaurantCommands.cs        # (existing) Test commands for authorization
│   ├── TestUserCommands.cs              # (existing) Test commands for user authorization
│   ├── RestaurantCommandAuthorizationTests.cs # (existing) Restaurant authorization tests
│   ├── UserCommandAuthorizationTests.cs # (existing) User authorization tests
│   ├── PolicyBasedAuthorizationTests.cs # (existing) Policy-based authorization tests
│   ├── MixedResourceAuthorizationTests.cs # (existing) Mixed resource authorization tests
│   └── DebugAuthorizationTests.cs       # (existing) Debug authorization tests
├── Features/
│   ├── Users/
│   │   ├── RegisterUserTests.cs         # (existing) User registration tests
│   │   └── DeviceManagementTests.cs     # (existing) Device management tests
│   ├── RoleAssignments/
│   │   └── [existing test files]        # (existing) Role assignment tests
│   ├── Notifications/
│   │   └── [existing test files]        # (existing) Notification tests
│   ├── TodoItems/
│   │   └── [existing test files]        # (existing) Todo item tests (template)
│   └── TodoLists/
│       └── [existing test files]        # (existing) Todo list tests (template)
├── Common/
│   ├── BaseTestFixture.cs               # (existing) Base test fixture
│   ├── ResultAssertions.cs              # (existing) Result assertion helpers
│   └── CustomWebApplicationFactory.cs   # (enhanced) Web application factory
├── Testing.cs                           # (refactored) Clean facade API
├── GlobalUsings.cs                      # (existing) Global using statements
├── Application.FunctionalTests.csproj  # (existing) Project file
└── appsettings.json                     # (existing) Test configuration
```

### Key Organizational Principles

#### 1. **Infrastructure Layer** (`Infrastructure/`)

- Contains core test infrastructure components
- Database-related classes grouped in `Database/` subfolder
- Configuration and setup utilities

#### 2. **User Management Layer** (`UserManagement/`)

- User creation and authentication logic
- Claims and authentication state management
- Separated from authorization concerns

#### 3. **Authorization Layer** (`Authorization/`)

- All authorization-related test helpers and utilities
- Existing authorization test files remain in this folder
- Restaurant-specific and general authorization helpers

#### 4. **Feature Tests** (`Features/`)

- Organized by domain/feature area
- Each feature has its own subfolder
- Mirrors the application's feature organization

#### 5. **Common Utilities** (`Common/`)

- Shared test utilities and base classes
- Enhanced web application factory
- Common assertion helpers

### File Migration Map

#### New Files to Create

- `Infrastructure/TestInfrastructure.cs` - Extract from `Testing.cs`
- `Infrastructure/TestDatabaseManager.cs` - Extract from `Testing.cs`
- `Infrastructure/TestConfiguration.cs` - New configuration class
- `UserManagement/TestUserManager.cs` - Extract from `Testing.cs`
- `UserManagement/TestAuthenticationService.cs` - Extract from `Testing.cs`
- `Authorization/RestaurantRoleTestHelper.cs` - Extract from `Testing.cs`
- `Authorization/AuthorizationTestSetup.cs` - Extract from `Testing.cs`

#### Files to Move

- `BaseTestFixture.cs` → `Common/BaseTestFixture.cs`
- `ResultAssertions.cs` → `Common/ResultAssertions.cs`
- `CustomWebApplicationFactory.cs` → `Common/CustomWebApplicationFactory.cs`
- All database-related files → `Infrastructure/Database/`
- All authorization test files → `Authorization/`
- Feature-specific test files → `Features/{FeatureName}/`

#### Files to Refactor

- `Testing.cs` - Becomes a clean facade that delegates to specialized classes
- `CustomWebApplicationFactory.cs` - Enhanced organization and service registration

## Implementation Strategy

### Phase 1: Extract Core Infrastructure

1. Create `TestInfrastructure.cs` with core setup/teardown logic
2. Create `TestDatabaseManager.cs` with database operations
3. Update existing tests to use new infrastructure classes

### Phase 2: Extract User Management

1. Create `TestUserManager.cs` with user creation and authentication
2. Create `TestAuthenticationService.cs` with claims management
3. Refactor user-related test methods

### Phase 3: Extract Authorization Helpers

1. Create `RestaurantRoleTestHelper.cs` with restaurant role logic
2. Create `AuthorizationTestSetup.cs` with common authorization setup
3. Update authorization tests to use new helpers

### Phase 4: Create Configuration Layer

1. Create `TestConfiguration.cs` with test constants and settings
2. Enhance `CustomWebApplicationFactory.cs` organization
3. Update all classes to use centralized configuration

### Phase 5: Create Unified Facade

1. Refactor `Testing.cs` to act as a clean facade
2. Ensure backward compatibility with existing tests
3. Update documentation and examples

### Phase 6: Cleanup and Optimization

1. Remove duplicate code
2. Optimize performance where possible
3. Add comprehensive documentation
4. Create usage examples and guidelines

## Migration Considerations

### Backward Compatibility

- Maintain existing public API in `Testing.cs` during transition
- Use `[Obsolete]` attributes to guide migration
- Provide clear migration path documentation

### Testing the Refactoring

- Run full test suite after each phase
- Ensure no functional changes in test behavior
- Verify performance is maintained or improved

### Documentation Updates

- Create `Application_Functional_Testing_Guidelines.md`
- Update existing documentation references
- Provide examples of new usage patterns

## Expected Outcomes

1. **Reduced Complexity**: Individual classes will be smaller and more focused
2. **Improved Developer Experience**: Easier to understand and use test infrastructure
3. **Better Test Organization**: Clear patterns for organizing different types of tests
4. **Enhanced Maintainability**: Easier to modify and extend test infrastructure
5. **Increased Reusability**: Components can be reused across different test scenarios
6. **Better Error Handling**: More specific error messages and better debugging experience

This refactoring will transform the monolithic `Testing.cs` file into a well-organized, maintainable test infrastructure that follows Clean Architecture principles and supports the long-term growth of the test suite.
