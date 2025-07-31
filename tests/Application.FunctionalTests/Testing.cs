using YummyZoom.SharedKernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.UserManagement;
using YummyZoom.Application.FunctionalTests.Authorization;

namespace YummyZoom.Application.FunctionalTests;

/// <summary>
/// Unified facade for functional test infrastructure.
/// Provides a clean, organized API for test setup, user management, authorization, and database operations.
/// </summary>
[SetUpFixture]
public partial class Testing
{
    #region Test Infrastructure Setup and Teardown

    /// <summary>
    /// Initializes the test infrastructure before any tests run.
    /// </summary>
    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        await TestInfrastructure.RunBeforeAnyTests();
    }

    /// <summary>
    /// Cleans up the test infrastructure after all tests complete.
    /// </summary>
    [OneTimeTearDown]
    public async Task RunAfterAnyTests()
    {
        await TestInfrastructure.RunAfterAnyTests();
    }

    /// <summary>
    /// Resets the test state between tests, including database and user context.
    /// </summary>
    public static async Task ResetState()
    {
        await TestInfrastructure.ResetState();
        TestUserManager.ClearUserContext();
    }

    #endregion

    #region Command and Query Execution

    /// <summary>
    /// Sends a request and returns the response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <returns>The response from the request.</returns>
    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        return await TestInfrastructure.SendAsync(request);
    }

    /// <summary>
    /// Sends a request that returns a Result&lt;T&gt; and unwraps the value.
    /// </summary>
    /// <typeparam name="T">The value type wrapped in the Result.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <returns>The unwrapped value from the Result.</returns>
    public static async Task<T> SendAndUnwrapAsync<T>(IRequest<Result<T>> request)
    {
        return await TestInfrastructure.SendAndUnwrapAsync(request);
    }

    /// <summary>
    /// Sends a request without expecting a response.
    /// </summary>
    /// <param name="request">The request to send.</param>
    public static async Task SendAsync(IBaseRequest request)
    {
        await TestInfrastructure.SendAsync(request);
    }

    #endregion

    #region User Management and Authentication

    /// <summary>
    /// Gets the current user ID from the test context.
    /// </summary>
    /// <returns>The current user ID, or null if no user is set.</returns>
    public static Guid? GetUserId() 
    {
        return TestUserManager.GetCurrentUserId();
    }

    /// <summary>
    /// Sets the current user ID in the test context.
    /// </summary>
    /// <param name="userId">The user ID to set as current.</param>
    public static void SetUserId(Guid? userId)
    {
        TestUserManager.SetCurrentUserId(userId);
    }

    /// <summary>
    /// Refreshes the current user's claims from the database.
    /// </summary>
    public static async Task RefreshUserClaimsAsync()
    {
        await TestAuthenticationService.RefreshUserClaimsAsync();
    }

    /// <summary>
    /// Creates and runs as the default test user.
    /// </summary>
    /// <returns>The ID of the created user.</returns>
    public static async Task<Guid> RunAsDefaultUserAsync() 
    {
        return await TestUserManager.RunAsDefaultUserAsync();
    }

    /// <summary>
    /// Creates and runs as an administrator user.
    /// </summary>
    /// <returns>The ID of the created administrator user.</returns>
    public static async Task<Guid> RunAsAdministratorAsync() 
    {
        return await TestUserManager.RunAsAdministratorAsync();
    }

    /// <summary>
    /// Creates and runs as a user with specified credentials and roles.
    /// </summary>
    /// <param name="userName">The username for the user.</param>
    /// <param name="password">The password for the user.</param>
    /// <param name="roles">The roles to assign to the user.</param>
    /// <returns>The ID of the created user.</returns>
    public static async Task<Guid> RunAsUserAsync(string userName, string password, string[] roles) 
    {
        return await TestUserManager.RunAsUserAsync(userName, password, roles);
    }

    /// <summary>
    /// Creates a new user with specified credentials and roles.
    /// </summary>
    /// <param name="email">The email for the user.</param>
    /// <param name="password">The password for the user.</param>
    /// <param name="roles">The roles to assign to the user.</param>
    /// <returns>The ID of the created user.</returns>
    public static async Task<Guid> CreateUserAsync(string email, string password, params string[] roles)
    {
        return await TestUserManager.CreateUserAsync(email, password, roles);
    }

    /// <summary>
    /// Ensures that the specified roles exist in the system.
    /// </summary>
    /// <param name="roleNames">The role names to ensure exist.</param>
    public static async Task EnsureRolesExistAsync(params string[]? roleNames)
    {
        await TestUserManager.EnsureRolesExistAsync(roleNames);
    }

    /// <summary>
    /// Sets up the test environment for user registration tests.
    /// </summary>
    public static async Task SetupForUserRegistrationTestsAsync()
    {
        await TestUserManager.SetupForUserRegistrationTestsAsync();
    }

    #endregion

    #region Database Operations

    /// <summary>
    /// Finds an entity by its key values.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="keyValues">The key values to search for.</param>
    /// <returns>The found entity, or null if not found.</returns>
    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        return await TestDatabaseManager.FindAsync<TEntity>(keyValues);
    }

    /// <summary>
    /// Adds an entity to the database.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to add.</param>
    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        await TestDatabaseManager.AddAsync(entity);
    }

    /// <summary>
    /// Counts the number of entities of the specified type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The count of entities.</returns>
    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        return await TestDatabaseManager.CountAsync<TEntity>();
    }

    #endregion

    #region Service Access

    /// <summary>
    /// Gets a service from the test service provider.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The requested service.</returns>
    public static T GetService<T>()
        where T : notnull
    {
        return TestInfrastructure.GetService<T>();
    }

    /// <summary>
    /// Creates a new service scope for dependency injection.
    /// </summary>
    /// <returns>A new service scope.</returns>
    public static IServiceScope CreateScope()
    {
        return TestInfrastructure.CreateScope();
    }

    #endregion

    #region Authorization and Restaurant Roles

    /// <summary>
    /// Creates a role assignment for a user in a restaurant with the specified role.
    /// Requires an administrator to be logged in.
    /// </summary>
    /// <param name="userId">The ID of the user to assign the role to.</param>
    /// <param name="restaurantId">The ID of the restaurant.</param>
    /// <param name="role">The restaurant role to assign.</param>
    /// <returns>The ID of the created role assignment.</returns>
    public static async Task<Guid> CreateRoleAssignmentAsync(Guid userId, Guid restaurantId, RestaurantRole role)
    {
        return await RestaurantRoleTestHelper.CreateRoleAssignmentAsync(userId, restaurantId, role);
    }

    /// <summary>
    /// Sets up a user as a restaurant owner for the specified restaurant.
    /// Creates the user, assigns administrator role temporarily to create role assignment, then switches to the user.
    /// </summary>
    /// <param name="email">The email for the restaurant owner user.</param>
    /// <param name="restaurantId">The ID of the restaurant to own.</param>
    /// <returns>The ID of the created restaurant owner user.</returns>
    public static async Task<Guid> RunAsRestaurantOwnerAsync(string email, Guid restaurantId)
    {
        return await RestaurantRoleTestHelper.RunAsRestaurantOwnerAsync(email, restaurantId);
    }

    /// <summary>
    /// Sets up a user as restaurant staff for the specified restaurant.
    /// Creates the user, assigns administrator role temporarily to create role assignment, then switches to the user.
    /// </summary>
    /// <param name="email">The email for the restaurant staff user.</param>
    /// <param name="restaurantId">The ID of the restaurant to work for.</param>
    /// <returns>The ID of the created restaurant staff user.</returns>
    public static async Task<Guid> RunAsRestaurantStaffAsync(string email, Guid restaurantId)
    {
        return await RestaurantRoleTestHelper.RunAsRestaurantStaffAsync(email, restaurantId);
    }

    /// <summary>
    /// Sets up a user with multiple restaurant roles for testing complex authorization scenarios.
    /// </summary>
    /// <param name="email">The email for the user.</param>
    /// <param name="roleAssignments">Array of restaurant ID and role pairs to assign.</param>
    /// <returns>The ID of the created user with multiple roles.</returns>
    public static async Task<Guid> RunAsUserWithMultipleRestaurantRolesAsync(string email, (Guid restaurantId, RestaurantRole role)[] roleAssignments)
    {
        return await RestaurantRoleTestHelper.RunAsUserWithMultipleRestaurantRolesAsync(email, roleAssignments);
    }

    /// <summary>
    /// Sets up authorization test environment with required roles.
    /// </summary>
    public static async Task SetupForAuthorizationTestsAsync()
    {
        await AuthorizationTestSetup.SetupForAuthorizationTestsAsync();
    }

    #endregion
}
