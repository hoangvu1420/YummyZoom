## Refactoring Query Handlers to Use Dapper

### High-Level Plan: Refactoring Query Handlers to Use Dapper

This plan is structured in phases. The goal is to establish a robust, reusable pattern for all query handlers in the `Application` layer, using the `GetRestaurantRoleAssignmentsQuery` as our primary example.

#### Phase 1: Establish the Foundation (Infrastructure and Application Setup)

This phase involves setting up the necessary tools and abstractions so that query handlers can get a database connection without being tied to a specific implementation.

1. **Add Dapper Dependency:**
    * **Action:** Add the Dapper NuGet package.
    * **File to Modify:** `src/Infrastructure/Infrastructure.csproj`
    * **Reasoning:** Dapper is a data access implementation detail. The `Infrastructure` project is the correct place for this dependency.

2. **Define a Connection Factory Interface:**
    * **Action:** Create a new interface `IDbConnectionFactory` that defines a contract for creating a `DbConnection`.
    * **File to Create:** `src/Application/Common/Interfaces/IDbConnectionFactory.cs`
    * **Reasoning:** This follows the Dependency Inversion Principle. The `Application` layer defines the abstraction it needs, and `Infrastructure` will provide the implementation. This keeps the `Application` layer independent of connection management details.
    * **Code:**

        ```csharp
        using System.Data;

        namespace YummyZoom.Application.Common.Interfaces;

        public interface IDbConnectionFactory : IDisposable
        {
            IDbConnection CreateConnection();
        }
        ```

3. **Implement the Connection Factory:**
    * **Action:** Create a concrete implementation of `IDbConnectionFactory` that uses the connection string from your application's configuration.
    * **File to Create:** `src/Infrastructure/Data/DbConnectionFactory.cs`
    * **Reasoning:** This class contains the infrastructure-specific logic for creating a database connection (e.g., `NpgsqlConnection` for PostgreSQL or `SqlConnection` for SQL Server).
    * **Code:**

        ```csharp
        using System.Data;
        using Microsoft.Extensions.Configuration;
        using Npgsql; // Or System.Data.SqlClient for SQL Server

        namespace YummyZoom.Infrastructure.Data;

        public class DbConnectionFactory : IDbConnectionFactory
        {
            private readonly IConfiguration _configuration;
            private IDbConnection? _connection;

            public DbConnectionFactory(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public IDbConnection CreateConnection()
            {
                if (_connection is null || _connection.State != ConnectionState.Open)
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection");
                    _connection = new NpgsqlConnection(connectionString); // Or new SqlConnection(connectionString);
                    _connection.Open();
                }
                return _connection;
            }

            public void Dispose()
            {
                if (_connection is not null && _connection.State == ConnectionState.Open)
                {
                    _connection.Dispose();
                }
            }
        }
        ```

4. **Register the Service:**
    * **Action:** Register `IDbConnectionFactory` with the dependency injection container as a scoped service.
    * **File to Modify:** A new `DependencyInjection.cs` file in the `Infrastructure` project.
    * **Reasoning:** This makes the connection factory available to all query handlers throughout the application. A scoped lifetime ensures that the same connection is used for the duration of a single HTTP request.
    * **Code (in `src/Infrastructure/DependencyInjection.cs`):**

        ```csharp
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using YummyZoom.Application.Common.Interfaces;
        using YummyZoom.Infrastructure.Data;

        namespace YummyZoom.Infrastructure;

        public static class DependencyInjection
        {
            public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
            {
                // ... other infrastructure registrations
                services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
                return services;
            }
        }
        ```

#### Phase 2: Refactor the Target Query Handler

Now we apply the new pattern to `GetRestaurantRoleAssignmentsQueryHandler`.

1. **Change the Dependency:**
    * **Action:** Modify the constructor of `GetRestaurantRoleAssignmentsQueryHandler` to inject `IDbConnectionFactory` instead of `IRoleAssignmentRepository`.

2. **Rewrite the `Handle` Method:**
    * **Action:** Replace the repository call and in-memory mapping with a direct Dapper query. The SQL query will be crafted to return the exact shape of the `RoleAssignmentDto`.
    * **File to Modify:** `src/Application/RoleAssignments/Queries/GetRestaurantRoleAssignments/GetRestaurantRoleAssignments.cs`
    * **Refactored Code:**

        ```csharp
        using Dapper; // Add this using statement
        using YummyZoom.Application.Common.Interfaces; // For IDbConnectionFactory
        using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
        using YummyZoom.SharedKernel;

        namespace YummyZoom.Application.RoleAssignments.Queries.GetRestaurantRoleAssignments;

        // ... DTOs and Query record remain the same ...

        public class GetRestaurantRoleAssignmentsQueryHandler : IRequestHandler<GetRestaurantRoleAssignmentsQuery, Result<GetRestaurantRoleAssignmentsResponse>>
        {
            private readonly IDbConnectionFactory _dbConnectionFactory;

            // Dependency has changed
            public GetRestaurantRoleAssignmentsQueryHandler(IDbConnectionFactory dbConnectionFactory)
            {
                _dbConnectionFactory = dbConnectionFactory;
            }

            public async Task<Result<GetRestaurantRoleAssignmentsResponse>> Handle(GetRestaurantRoleAssignmentsQuery request, CancellationToken cancellationToken)
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                const string sql = @"
                    SELECT
                        ra.""Id"",
                        ra.""UserId"",
                        ra.""RestaurantId"",
                        ra.""Role"",
                        ra.""CreatedAt"",
                        ra.""UpdatedAt""
                    FROM ""RoleAssignments"" AS ra
                    WHERE ra.""RestaurantId"" = @RestaurantId";

                // Dapper executes the query and maps the result directly to the DTO
                var roleAssignments = await connection.QueryAsync<RoleAssignmentDto>(
                    new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

                return Result.Success(new GetRestaurantRoleAssignmentsResponse(roleAssignments.AsList()));
            }
        }
        ```

    * **Key Benefits Realized:** No full aggregate loading, no in-memory projection, and the query is explicit and optimized for this specific use case.

#### Phase 3: Generalize and Document the Pattern

This final phase ensures the new approach is adopted consistently.

1. **Document the Standard:**
    * **Action:** Add comments or internal documentation stating the new convention:
        * **Commands** use Repositories to work with full aggregates.
        * **Queries** use `IDbConnectionFactory` and Dapper to fetch DTOs directly.
        * The `CommandHandlers` (like `CreateRoleAssignmentCommandHandler`) **should not change**. They are correctly using the repository pattern.

2. **Apply to More Complex Queries:**
    * **Action:** When creating new query handlers that require data from multiple aggregates (e.g., getting a `Restaurant` with its average `Rating`), the Dapper query will simply use a `JOIN`.
    * **Example (Hypothetical):**

        ```sql
        SELECT r.Name, r.Description, s.AverageRating
        FROM Restaurants AS r
        LEFT JOIN RestaurantRatingSummaries AS s ON r.Id = s.RestaurantId
        WHERE r.Id = @RestaurantId
        ```

    This demonstrates the true power of this approach—it's incredibly flexible and avoids the complexity of trying to make EF Core `Include()` statements work for complex, flattened DTOs.

By following this plan, you will successfully transition your application's read-side to a more performant, scalable, and maintainable pattern that fully embraces CQRS principles.
